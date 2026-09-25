using System.Diagnostics;
using System.Linq;
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

namespace Content.Server.Toolshed.Botany;

[ToolshedCommand, AdminCommand(AdminFlags.Spawn | AdminFlags.VarEdit)]
public sealed partial class PlantCommand : ToolshedCommand
{
    private const int MaxAgeTicks = 1000;
    private static readonly EntProtoId TrayPrototype = "hydroponicsTray";
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private PlantMutationSystem? _mutationSystem;
    private BotanySystem? _botany;
    private PlantChemicalsSystem? _plantChemicals;
    private PlantHolderSystem? _plantHolder;
    private PlantSystem? _plant;
    private PlantTraySystem? _plantTray;

    private EntityUid? SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] EntityCoordinates coordinates,
        EntProtoId seedPrototype)
    {
        if (!_prototypeManager.TryIndex(seedPrototype, out var seedProto) ||
            !seedProto.TryComp<SeedComponent>(out var seedComponent, EntityManager.ComponentFactory))
        {
            ctx.ReportError(new PlantCommandError(
                $"Prototype '{seedPrototype}' is not a seed."));
            return null;
        }

        var tray = Spawn(TrayPrototype, coordinates);
        if (!TryComp<PlantTrayComponent>(tray, out var trayComponent))
        {
            QDel(tray);
            ctx.ReportError(new PlantCommandError($"Prototype '{TrayPrototype}' did not create a plant tray."));
            return null;
        }

        var plant = Spawn(seedComponent.PlantProtoId, coordinates);
        _botany ??= GetSys<BotanySystem>();
        _botany.ApplyPlantSnapshotData(seedComponent.PlantData, plant);

        if (!HasComp<PlantComponent>(plant) || !HasComp<PlantHolderComponent>(plant))
        {
            QDel(plant);
            QDel(tray);
            ctx.ReportError(new PlantCommandError(
                $"Seed prototype '{seedPrototype}' did not create a valid plant entity."));
            return null;
        }

        _plantTray ??= GetSys<PlantTraySystem>();
        _plantTray.PlantingPlantInTray((tray, trayComponent), plant);
        return plant;
    }

    private EntityUid? SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] EntityUid target,
        EntProtoId seedPrototype)
    {
        return SpawnPlant(ctx, Transform(target).Coordinates, seedPrototype);
    }

    [CommandImplementation("spawn")]
    public IEnumerable<EntityUid> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityCoordinates> coordinates,
        EntProtoId seedPrototype)
    {
        return coordinates
            .Select(coordinate => SpawnPlant(ctx, coordinate, seedPrototype))
            .OfType<EntityUid>();
    }

    [CommandImplementation("spawn")]
    public IEnumerable<EntityUid> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        EntProtoId seedPrototype)
    {
        return targets
            .Select(target => SpawnPlant(ctx, target, seedPrototype))
            .OfType<EntityUid>();

    }

    private EntityUid? Age(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        int ticks)
    {
        if (ticks is < 0 or > MaxAgeTicks)
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant growth ticks must be between 0 and {MaxAgeTicks}."));
            return null;
        }

        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return null;


        AgeTicks((input, plant), (input, holder), ticks);

        return input;
    }

    [CommandImplementation("age")]
    public IEnumerable<EntityUid> Age(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        int ticks)
    {
        return input
            .Select(entity => Age(ctx, entity, ticks))
            .OfType<EntityUid>();

    }

    [CommandImplementation("ageuntilready")]
    public EntityUid? AgeUntilReady(IInvocationContext ctx, [PipedArgument] EntityUid input)
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
        AgeTicks((input, plant), (input, holder), readyAge - holder.Age);

        return input;
    }

    [CommandImplementation("ageuntilready")]
    public IEnumerable<EntityUid> AgeUntilReady(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input)
    {
        return input
            .Select(entity => AgeUntilReady(ctx, entity))
            .OfType<EntityUid>();
    }

    public EntityUid? AddMutation(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        ProtoId<RandomPlantMutationListPrototype> tableId,
        [CommandArgument(typeof(PlantMutationNameParser))]
        string mutationName)
    {
        if (!TryComp<PlantComponent>(input, out var plant))
        {
            ctx.ReportError(new PlantCommandError($"Entity {input} is not a plant."));
            return null;
        }

        var table = _prototypeManager.Index(tableId);
        var mutation = table.Mutations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, mutationName, StringComparison.OrdinalIgnoreCase));

        if (mutation == null)
        {
            ctx.ReportError(new PlantCommandError(
                $"Mutation '{mutationName}' does not exist in mutation table '{tableId}'."));
            return EntityUid.Invalid;
        }

        _mutationSystem ??= GetSys<PlantMutationSystem>();
        if (!_mutationSystem.TryAddMutation((input, plant), mutation))
        {
            ctx.ReportError(new PlantCommandError(
                $"Mutation '{mutation.Name}' is already present or conflicts with another mutation on entity {input}."));
            return EntityUid.Invalid;
        }

        return input;
    }

    [CommandImplementation("addmutation")]
    public IEnumerable<EntityUid> AddMutation(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        ProtoId<RandomPlantMutationListPrototype> tableId,
        [CommandArgument(typeof(PlantMutationNameParser))]
        string mutationName)
    {
        return input
            .Select(entity => AddMutation(ctx, entity, tableId, mutationName))
            .OfType<EntityUid>();
    }

    [CommandImplementation("addchem")]
    public EntityUid AddChemical(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        ProtoId<ReagentPrototype> reagent,
        float min,
        float amount,
        bool inherent = false)
    {
        if (!TryGetPlant(input, ctx, out _, out _))
            return EntityUid.Invalid;

        if (!float.IsFinite(min) || !float.IsFinite(amount) || min < 0f || amount < 0f || min + amount <= 0)
        {
            ctx.ReportError(new PlantCommandError(
                $"Chemical quantities must be finite, with min and max both non-negative and at least one nonzero."));
            return EntityUid.Invalid;
        }

        _plantChemicals ??= GetSys<PlantChemicalsSystem>();
        if (!_plantChemicals.AddChemical(input, reagent, FixedPoint2.New(min), FixedPoint2.New(amount), inherent))
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant entity {input} did not accept added chemicals."));
            return EntityUid.Invalid;
        }

        return input;
    }

    [CommandImplementation("addchem")]
    public IEnumerable<EntityUid> AddChemical(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        ProtoId<ReagentPrototype> reagent,
        float min,
        float amount,
        bool inherent = false)
    {
        return input
            .Select(entity => AddChemical(ctx, entity, reagent, min, amount, inherent))
            .Where(entity => entity.IsValid());
    }

    private bool TryGetPlant(
        EntityUid input,
        IInvocationContext ctx,
        out PlantComponent plant,
        out PlantHolderComponent holder)
    {
        plant = default!;
        holder = default!;
        if (!TryComp<PlantComponent>(input, out var plantComponent) ||
            !TryComp<PlantHolderComponent>(input, out var holderComponent))
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

    private bool AgeTicks(Entity<PlantComponent> plant, Entity<PlantHolderComponent> holder, int ticks)
    {
        _plantHolder ??= GetSys<PlantHolderSystem>();
        _plant ??= GetSys<PlantSystem>();

        _plantHolder.AdjustsAge(holder.AsNullable(), ticks);
        _plant.ForceUpdate(plant.AsNullable());
        return !Deleted(plant) && !_plantHolder.IsDead(holder.AsNullable());
    }
}


public record struct PlantCommandError(string Message) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(Message);
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed partial class PlantMutationNameParser : CustomCompletionParser<string>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override CompletionResult TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        var options = _prototypeManager
            .EnumeratePrototypes<RandomPlantMutationListPrototype>()
            .SelectMany(table => table.Mutations)
            .Select(mutation => mutation.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .Select(name => new CompletionOption(
                $"\"{name}\"",
                Flags: CompletionOptionFlags.NoEscape));

        return CompletionResult.FromHintOptions(options, "<mutation name>");
    }
}
