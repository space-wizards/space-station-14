using System.Diagnostics;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server.Botany;

[ToolshedCommand, AdminCommand(AdminFlags.Spawn | AdminFlags.VarEdit)]
public sealed partial class PlantCommand : ToolshedCommand
{
    private const int MaxAgeTicks = 1000;
    private static readonly EntProtoId TrayPrototype = "hydroponicsTray";
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private PlantMutationSystem? _mutationSystem;
    private BotanySystem? _botany;
    private PlantHolderSystem? _plantHolder;
    private PlantSystem? _plant;
    private PlantTraySystem? _plantTray;

    [CommandImplementation("spawn")]
    public EntityUid SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] EntityCoordinates coordinates,
        EntProtoId seedPrototype)
    {
        var tray = Spawn(TrayPrototype, coordinates);
        if (!TryComp<PlantTrayComponent>(tray, out var trayComponent))
        {
            QDel(tray);
            ctx.ReportError(new PlantCommandError($"Prototype '{TrayPrototype}' did not create a plant tray."));
            return EntityUid.Invalid;
        }

        var seed = Spawn(seedPrototype, coordinates);
        if (!TryComp<SeedComponent>(seed, out var seedComponent))
        {
            QDel(seed);
            QDel(tray);
            ctx.ReportError(new PlantCommandError($"Prototype '{seedPrototype}' is not a seed."));
            return EntityUid.Invalid;
        }

        var plant = Spawn(seedComponent.PlantProtoId, coordinates);
        _botany ??= GetSys<BotanySystem>();
        _botany.ApplyPlantSnapshotData(seedComponent.PlantData, plant);

        if (!HasComp<PlantComponent>(plant) || !HasComp<PlantHolderComponent>(plant))
        {
            QDel(seed);
            QDel(plant);
            QDel(tray);
            ctx.ReportError(new PlantCommandError(
                $"Seed prototype '{seedPrototype}' did not create a valid plant entity."));
            return EntityUid.Invalid;
        }

        _plantTray ??= GetSys<PlantTraySystem>();
        _plantTray.PlantingPlantInTray((tray, trayComponent), plant, seedComponent.HealthOverride);
        QDel(seed);
        return plant;
    }

    [CommandImplementation("spawn")]
    public EntityUid SpawnPlant(
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
            .Where(entity => entity.IsValid());
    }

    [CommandImplementation("spawn")]
    public IEnumerable<EntityUid> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        EntProtoId seedPrototype)
    {
        return targets
            .Select(target => SpawnPlant(ctx, target, seedPrototype))
            .Where(entity => entity.IsValid());
    }

    [CommandImplementation("age")]
    public EntityUid Age(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        int ticks)
    {
        if (ticks < 0 || ticks > MaxAgeTicks)
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant growth ticks must be between 0 and {MaxAgeTicks}."));
            return EntityUid.Invalid;
        }

        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return EntityUid.Invalid;

        for (var i = 0; i < ticks; i++)
        {
            if (!AgeOneTick((input, plant), (input, holder)))
            {
                ctx.ReportError(new PlantCommandError(
                    $"Plant entity {input} died after {i + 1} growth ticks."));
                return EntityUid.Invalid;
            }
        }

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
            .Where(entity => entity.IsValid());
    }

    [CommandImplementation("ageuntilready")]
    public EntityUid AgeUntilReady(IInvocationContext ctx, [PipedArgument] EntityUid input)
    {
        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return EntityUid.Invalid;

        if (!HasComp<PlantHarvestComponent>(input))
        {
            ctx.ReportError(new PlantCommandError($"Plant entity {input} cannot produce a harvest."));
            return EntityUid.Invalid;
        }

        for (var i = 0; i < MaxAgeTicks && !holder.ReadyForHarvest; i++)
        {
            if (!AgeOneTick((input, plant), (input, holder)))
            {
                ctx.ReportError(new PlantCommandError(
                    $"Plant entity {input} died before it became ready to harvest."));
                return EntityUid.Invalid;
            }
        }

        if (!holder.ReadyForHarvest)
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant entity {input} was not ready to harvest after {MaxAgeTicks} growth ticks."));
            return EntityUid.Invalid;
        }

        return input;
    }

    [CommandImplementation("ageuntilready")]
    public IEnumerable<EntityUid> AgeUntilReady(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input)
    {
        return input
            .Select(entity => AgeUntilReady(ctx, entity))
            .Where(entity => entity.IsValid());
    }

    [CommandImplementation("addmutation")]
    public EntityUid Add(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        ProtoId<RandomPlantMutationListPrototype> tableId,
        [CommandArgument(typeof(PlantMutationNameParser))]
        string mutationName)
    {
        if (!TryComp<PlantComponent>(input, out var plant))
        {
            ctx.ReportError(new PlantCommandError($"Entity {input} is not a plant."));
            return EntityUid.Invalid;
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
    public IEnumerable<EntityUid> Add(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        ProtoId<RandomPlantMutationListPrototype> tableId,
        [CommandArgument(typeof(PlantMutationNameParser))]
        string mutationName)
    {
        foreach (var entity in input)
        {
            var mutated = Add(ctx, entity, tableId, mutationName);
            if (mutated.IsValid())
                yield return mutated;
        }
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

    private bool AgeOneTick(Entity<PlantComponent> plant, Entity<PlantHolderComponent> holder)
    {
        _plantHolder ??= GetSys<PlantHolderSystem>();
        _plant ??= GetSys<PlantSystem>();

        _plantHolder.AdjustsAge(holder.AsNullable(), 1);
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

    public override Robust.Shared.Console.CompletionResult TryAutocomplete(
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
