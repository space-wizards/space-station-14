using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Botany.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Spawn | AdminFlags.VarEdit)]
public sealed partial class PlantCommand : ToolshedCommand
{
    private static readonly EntProtoId TrayPrototype = "HydroponicsTray";

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    private PlantMutationSystem? _mutationSystem;
    private BotanySystem? _botany;
    private PlantChemicalsSystem? _plantChemicals;
    private PlantHolderSystem? _plantHolder;
    private PlantSystem? _plant;
    private PlantTraySystem? _plantTray;

    [CommandImplementation("spawn")]
    public IEnumerable<Entity<PlantComponent>> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityCoordinates> coordinates,
        [CommandArgument(typeof(PlantSeedPrototypeParser))] EntProtoId seedPrototype)
    {
        return coordinates.Select(coordinate => SpawnPlant(ctx, coordinate, seedPrototype))
                          .CastNotNullOnly();
    }

    [CommandImplementation("spawn")]
    public IEnumerable<Entity<PlantComponent>> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument(typeof(PlantSeedPrototypeParser))] EntProtoId seedPrototype)
    {
        return targets.Select(target => SpawnPlant(ctx, target, seedPrototype))
                      .CastNotNullOnly();
    }

    [CommandImplementation("age")]
    public IEnumerable<Entity<PlantComponent>> Age(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<Entity<PlantComponent>> input,
        int ticks)
    {
        if (ticks <= 0)
        {
            ctx.ReportError(new PlantCommandError(
                $"Cannot age by {ticks}, can only accept positive number."));
            return [];
        }

        return input.Select(entity => Age(ctx, entity, ticks))!
                    .CastNotNullOnly();
    }

    [CommandImplementation("ageuntilready")]
    public IEnumerable<Entity<PlantComponent>> AgeUntilReady(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<Entity<PlantComponent>> input)
    {
        return input.Select(entity => AgeUntilReady(ctx, entity))!
                    .CastNotNullOnly();
    }

    [CommandImplementation("addmutation")]
    public IEnumerable<Entity<PlantComponent>> AddMutation(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<Entity<PlantComponent>> input,
        [CommandArgument(typeof(PlantMutationNameParser))]
        PlantMutationSelector plantMutation
    )
    {
        return input.Select(entity => AddMutation(ctx, entity, plantMutation.TableId, plantMutation.MutationName))!
                    .CastNotNullOnly();
    }

    [CommandImplementation("addchem")]
    public IEnumerable<Entity<PlantComponent>> AddChemical(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<Entity<PlantComponent>> input,
        ProtoId<ReagentPrototype> reagent,
        float min,
        float amount,
        bool inherent = false)
    {
        return input.Select(entity => AddChemical(ctx, entity, reagent, min, amount, inherent))!
                    .CastNotNullOnly();
    }

    public Entity<PlantComponent>? AgeUntilReady(IInvocationContext ctx, [PipedArgument] Entity<PlantComponent> input)
    {
        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return null;

        if (!HasComp<PlantHarvestComponent>(input))
        {
            ctx.ReportError(new PlantCommandError($"Plant entity {input} cannot produce a harvest."));
            return null;
        }

        if (holder.ReadyForHarvest)
            return input; //Why'd you even call this command...

        int readyAge;

        if (holder.Age < plant.Maturation)
        {
            readyAge = (int)MathF.Ceiling(plant.Maturation) + (int)MathF.Floor(plant.Production) + 1;
        }
        else
        {
            readyAge = holder.LastHarvest + (int)MathF.Floor(plant.Production) + 1;
        }

        TryAgeTicks((input, plant), (input, holder), readyAge - holder.Age);

        return input;
    }

    public Entity<PlantComponent>? AddChemical(
        IInvocationContext ctx,
        [PipedArgument] Entity<PlantComponent> input,
        ProtoId<ReagentPrototype> reagent,
        float min,
        float amount,
        bool inherent = false)
    {
        if (!TryGetPlant(input, ctx, out _, out _))
            return null;

        _plantChemicals ??= GetSys<PlantChemicalsSystem>();
        if (!_plantChemicals.AddChemical(input.Owner, reagent, FixedPoint2.New(min), FixedPoint2.New(amount), inherent))
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant entity {input} did not accept added chemicals."));
            return null;
        }
        
        return input;
    }

    public Entity<PlantComponent>? AddMutation(
        IInvocationContext ctx,
        [PipedArgument] Entity<PlantComponent> input,
        ProtoId<RandomPlantMutationListPrototype> tableId,
        string mutationName)
    {
        if (!TryGetPlant(input, ctx, out var plant, out _))
            return null;

        var table = _prototypeManager.Index(tableId);
        var mutation = table.Mutations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, mutationName, StringComparison.OrdinalIgnoreCase));

        //This shouldn't ever happen because the parser *should* catch it
        if (mutation == null)
            return null;

        _mutationSystem ??= GetSys<PlantMutationSystem>();
        if (!_mutationSystem.TryAddMutation((input, plant), mutation))
        {
            ctx.ReportError(new PlantCommandError(
                $"Mutation '{mutation.Name}' is already present or conflicts with another mutation on entity {input}."));
            return null;
        }

        return input;
    }

    private Entity<PlantComponent>? SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] EntityCoordinates coordinates,
        EntProtoId seedPrototype)
    {
        var seedProto = _prototypeManager.Index(seedPrototype);
        if (!seedProto.TryComp<SeedComponent>(out var seedComponent, _componentFactory))
            return null;

        var tray = Spawn(TrayPrototype, coordinates);
        if (!TryComp<PlantTrayComponent>(tray, out var trayComponent))
            return null;

        var plant = Spawn(seedComponent.PlantProtoId, coordinates);

        _botany ??= GetSys<BotanySystem>();
        _botany.ApplyPlantSnapshotData(seedComponent.PlantData, plant);

        _plantTray ??= GetSys<PlantTraySystem>();
        _plantTray.PlantingPlantInTray((tray, trayComponent), plant);

        if(!EntityManager.TryGetComponent(plant, out PlantComponent? plantComponent))
            return null;

        return (plant, plantComponent);
    }

    private bool TryGetPlant(
        EntityUid input,
        IInvocationContext ctx,
        [NotNullWhen(true)] out PlantComponent? plant,
        [NotNullWhen(true)] out PlantHolderComponent? holder)
    {
        plant = null;
        holder = null;
        if (!TryComp<PlantComponent>(input, out var plantComponent)
            || !TryComp<PlantHolderComponent>(input, out var holderComponent))
        {
            ctx.ReportError(new PlantCommandError($"Entity {input} is not a plant."));
            return false;
        }

        plant = plantComponent;
        holder = holderComponent;

        _plantHolder ??= GetSys<PlantHolderSystem>();
        if (_plantHolder.IsDead((input, holder)))
        {
            ctx.ReportError(new PlantCommandError($"Plant entity {input} is dead."));
            return false;
        }

        return true;
    }

    private Entity<PlantComponent>? Age(
        IInvocationContext ctx,
        [PipedArgument] Entity<PlantComponent> input,
        int ticks)
    {
        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return null;

        TryAgeTicks((input, plant), (input, holder), ticks);

        return input;
    }

    private bool TryAgeTicks(Entity<PlantComponent> plant, Entity<PlantHolderComponent> holder, int ticks)
    {
        _plantHolder ??= GetSys<PlantHolderSystem>();
        _plant ??= GetSys<PlantSystem>();

        _plantHolder.AdjustsAge(holder.AsNullable(), ticks);
        _plant.ForceUpdate(plant.AsNullable());
        return !Deleted(plant) && !_plantHolder.IsDead(holder.AsNullable());
    }

    private Entity<PlantComponent>? SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] EntityUid target,
        EntProtoId seedPrototype)
    {
        return SpawnPlant(ctx, Transform(target).Coordinates, seedPrototype);
    }

    private sealed class PlantCommandError(string message) : ConError
    {
        public override FormattedMessage DescribeInner()
        {
            return FormattedMessage.FromUnformatted(message);
        }
    }

    /// <summary> Parser for toolshed commands that can read out and provide autocomplete for seed prototypes. </summary>
    public sealed partial class PlantSeedPrototypeParser : CustomTypeParser<EntProtoId>
    {
        private static readonly ProtoId<EntityCategoryPrototype> SeedsCategoryId = "Seeds";

        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IComponentFactory _componentFactory = default!;

        /// <inheritdoc/>
        public override bool TryParse(ParserContext ctx, out EntProtoId result)
        {
            var start = ctx.Index;
            result = default;

            if (!Toolshed.TryParse(ctx, out EntProtoId? seed))
                return false;

            if (!_prototypeManager.TryIndex(seed, out var prototype))
            {
                ctx.Error = new PlantCommandError(
                    $"Couldn't find prototype '{result}'");
                ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
                return false;
            }

            if (!prototype.HasComp<SeedComponent>(_componentFactory))
            {
                ctx.Error = new PlantCommandError(
                    $"Prototype '{result}' isn't a seed.");
                ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));

                return false;
            }

            result = seed.Value;
            return true;
        }

        /// <inheritdoc/>
        public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));
            var completionOptions = CompletionHelper.EntityPrototypes(ctx.GetWord(), SeedsCategoryId, _prototypeManager);
            return CompletionResult.FromHintOptions(completionOptions, hint);
        }
    }

    /// <summary>
    /// Parser for toolshed commands that can read out and provide autocomplete for specific mutations.
    /// </summary>
    public sealed partial class PlantMutationNameParser : CustomTypeParser<PlantMutationSelector>
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        /// <inheritdoc/>
        public override bool TryParse(
            ParserContext ctx,
            [NotNullWhen(true)] out PlantMutationSelector? result)
        {
            var start = ctx.Index;
            result = null;
            if (!Toolshed.TryParse(ctx, out ProtoId<RandomPlantMutationListPrototype> table))
            {
                ctx.Error = new PlantCommandError("Failed to parse plant mutation list.");
                ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
                return false;
            }

            if (!Toolshed.TryParse<string>(ctx, out var name))
                return false;

            result = new(table, name);
            var foundMutation = _prototypeManager.Index(table)
                                                 .Mutations
                                                 .Any(mutation => string.Equals(
                                                     mutation.Name,
                                                     name,
                                                     StringComparison.OrdinalIgnoreCase)
                                                 );
            if (!foundMutation)
            {
                ctx.Error = new PlantCommandError($"Plant mutation '{name}' is not on list '{table}'.");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            if (!Toolshed.TryParse<ProtoId<RandomPlantMutationListPrototype>>(ctx, out var tableId))
            {
                return CompletionResult.FromHintOptions(
                    _prototypeManager.EnumeratePrototypes<RandomPlantMutationListPrototype>()
                                     .Select(prototype => new CompletionOption(prototype.ID)),
                    "<mutation table>"
                );
            }
            var options = _prototypeManager.Index(tableId)
                                           .Mutations.Select(mutation => mutation.Name)
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .OrderBy(name => name)
                                           .Select(name => new CompletionOption(
                                               $"\"{name}\"",
                                               Flags: CompletionOptionFlags.NoEscape)
                                           );

            return CompletionResult.FromHintOptions(options, "<mutation name>");
        }

    }
}


/// <summary>
/// Combination of Id for RandomPlantMutationListPrototype and name of mutation, which should uniquely identify a mutation.
/// </summary>
public record PlantMutationSelector(ProtoId<RandomPlantMutationListPrototype> TableId, string MutationName);

static file class LocalExtensions
{
    public static IEnumerable<Entity<PlantComponent>> CastNotNullOnly(this IEnumerable<Entity<PlantComponent>?> source)
    {
        return source.Where(x => x is { Comp: not null })
                     .Select(x => new Entity<PlantComponent>(x!.Value, x.Value!));
    }
}
