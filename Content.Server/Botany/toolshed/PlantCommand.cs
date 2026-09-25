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
    private static readonly EntProtoId TrayPrototype = "hydroponicsTray";
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

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
        var seedProto = _prototypeManager.Index(seedPrototype);
        if (!seedProto.TryComp<SeedComponent>(out var seedComponent, _componentFactory))
        {
            return null;
        }

        var tray = Spawn(TrayPrototype, coordinates);
        if (!TryComp<PlantTrayComponent>(tray, out var trayComponent))
        {
            return null;
        }

        var plant = Spawn(seedComponent.PlantProtoId, coordinates);
        _botany ??= GetSys<BotanySystem>();
        _botany.ApplyPlantSnapshotData(seedComponent.PlantData, plant);
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
        [CommandArgument(typeof(PlantSeedPrototypeParser))] EntProtoId seedPrototype)
    {
        return coordinates
            .Select(coordinate => SpawnPlant(ctx, coordinate, seedPrototype))
            .OfType<EntityUid>();
    }

    [CommandImplementation("spawn")]
    public IEnumerable<EntityUid> SpawnPlant(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> targets,
        [CommandArgument(typeof(PlantSeedPrototypeParser))] EntProtoId seedPrototype)
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
        if (!TryGetPlant(input, ctx, out var plant, out var holder))
            return null;

        AgeTicks((input, plant), (input, holder), ticks);

        return input;
    }

    [CommandImplementation("age")]
    public IEnumerable<EntityUid> Age(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument(typeof(PlantTicksParser))] int ticks)
    {
        return input
            .Select(entity => Age(ctx, entity, ticks))
            .OfType<EntityUid>();

    }

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
        string mutationName)
    {
        if (!TryGetPlant(input, ctx, out var plant, out var holder))
        {
            return null;
        }

        var table = _prototypeManager.Index(tableId);
        var mutation = table.Mutations.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, mutationName, StringComparison.OrdinalIgnoreCase));

        if (mutation == null)
        {
            //This shouldn't ever happen because the parser *should* catch it
            return null;
        }

        _mutationSystem ??= GetSys<PlantMutationSystem>();
        if (!_mutationSystem.TryAddMutation((input, plant), mutation))
        {
            ctx.ReportError(new PlantCommandError(
                $"Mutation '{mutation.Name}' is already present or conflicts with another mutation on entity {input}."));
            return null;
        }

        return input;
    }

    [CommandImplementation("addmutation")]
    public IEnumerable<EntityUid> AddMutation(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument(typeof(PlantMutationTableParser))] ProtoId<RandomPlantMutationListPrototype> tableId,
        [CommandArgument(typeof(PlantMutationNameParser))]
        string mutationName)
    {
        return input
            .Select(entity => AddMutation(ctx, entity, tableId, mutationName))
            .OfType<EntityUid>();
    }

    public EntityUid? AddChemical(
        IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        ProtoId<ReagentPrototype> reagent,
        [CommandArgument(typeof(PlantChemQuantityParser))] float min,
        [CommandArgument(typeof(PlantChemQuantityParser))] float amount,
        bool inherent = false)
    {
        if (!TryGetPlant(input, ctx, out _, out _))
            return null;

        _plantChemicals ??= GetSys<PlantChemicalsSystem>();
        if (!_plantChemicals.AddChemical(input, reagent, FixedPoint2.New(min), FixedPoint2.New(amount), inherent))
        {
            ctx.ReportError(new PlantCommandError(
                $"Plant entity {input} did not accept added chemicals."));
            return null;
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
            .OfType<EntityUid>();
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


public sealed class PlantCommandError(string message) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted(message);
    }
}

public sealed class PlantChemQuantityParser : CustomTypeParser<float>
{
    public override bool TryParse(ParserContext ctx, out float result)
    {
        var start = ctx.Index;

        if (!Toolshed.TryParse(ctx, out result))
            return false;

        if (!float.IsFinite(result))
        {
            ctx.Error = new PlantCommandError($"Plant chem quantities must be finite");
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        if (result is >= 0)
            return true;

        ctx.Error = new PlantCommandError($"Plant chem quantities must be non-negative");
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
        return false;
    }

    public override CompletionResult? TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        return CompletionResult.FromHint($"<quantity: A non-negative value>");
    }
}

public sealed class PlantTicksParser : CustomTypeParser<int>
{
    private const int MaxAgeTicks = 1000;
    public override bool TryParse(ParserContext ctx, out int result)
    {
        var start = ctx.Index;

        if (!Toolshed.TryParse(ctx, out result))
            return false;

        if (result is >= 0 and <= MaxAgeTicks)
            return true;

        ctx.Error = new PlantCommandError(
            $"Plant growth ticks must be between 0 and {MaxAgeTicks}.");

        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
        return false;
    }

    public override CompletionResult? TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        return CompletionResult.FromHint($"<ticks: 0-{MaxAgeTicks}>");
    }
}

public sealed partial class PlantSeedPrototypeParser : CustomTypeParser<EntProtoId>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    public override bool TryParse(
        ParserContext ctx,
        out EntProtoId result)
    {
        var start = ctx.Index;

        EntProtoId? seed;

        if (!Toolshed.TryParse(ctx, out seed))
        {
            ctx.Error = new PlantCommandError(
                $"Error parsing prototype '{seed}'.");
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            result = "";
            return false;
        }

        result = (EntProtoId)seed;

        if (!_prototypeManager.TryIndex(result, out EntityPrototype? prototype))
        {
            ctx.Error = new PlantCommandError(
                $"Couldn't find prototype '{result}'");
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            return false;
        }

        if (prototype.HasComp<SeedComponent>(_componentFactory))
            return true;

        ctx.Error = new PlantCommandError(
            $"Prototype '{result}' isn't a seed.");
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));

        return false;
    }
    public override CompletionResult TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        var options = _prototypeManager
            .EnumeratePrototypes<EntityPrototype>()
            .Where(prototype => prototype.HasComp<SeedComponent>(_componentFactory))
            .OrderBy(prototype => prototype.ID)
            .Select(prototype => new CompletionOption(prototype.ID));
        return CompletionResult.FromHintOptions(options, "<mutation name>");
    }
}

public sealed class PlantMutationTableParser
    : CustomCompletionParser<ProtoId<RandomPlantMutationListPrototype>>
{
    // Need to do this so that we can actually get the value when trying to parse the mutations
    public override bool EnableValueRef => false;

    public override CompletionResult? TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        return Toolshed.TryAutocomplete(
            ctx,
            typeof(ProtoId<RandomPlantMutationListPrototype>),
            arg);
    }
}

public sealed partial class PlantMutationNameParser : CustomTypeParser<string>
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override bool TryParse(
        ParserContext ctx,
        out string result)
    {
        var start = ctx.Index;

        string? name;

        if (!Toolshed.TryParse(ctx, out name))
        {
            result = "";
            return false;
        }

        result = name;
        var exists = false;
        if (ctx.Bundle.Arguments?.TryGetValue("tableId", out var mutationTable) is not true ||
            mutationTable is not ProtoId<RandomPlantMutationListPrototype> tableId)
        {
            exists = _prototypeManager
            .EnumeratePrototypes<RandomPlantMutationListPrototype>()
            .SelectMany(table => table.Mutations)
            .Any(mutation => string.Equals(
                mutation.Name,
                name,
                StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            exists = _prototypeManager
            .Index(tableId)
            .Mutations
            .Any(mutation => string.Equals(
                mutation.Name,
                name,
                StringComparison.OrdinalIgnoreCase));
        }

        if (exists)
            return true;

        ctx.Error = new PlantCommandError(
            $"Unknown plant mutation '{result}'.");
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));

        result = name;
        return false;
    }
    public override CompletionResult TryAutocomplete(
        ParserContext ctx,
        CommandArgument? arg)
    {
        IEnumerable<RandomPlantMutation> mutations;

        if (ctx.Bundle.Arguments?.TryGetValue("tableId", out var mutationTable) is not true ||
            mutationTable is not ProtoId<RandomPlantMutationListPrototype> tableId)
        {
            mutations = _prototypeManager
            .EnumeratePrototypes<RandomPlantMutationListPrototype>()
            .SelectMany(table => table.Mutations);
        }
        else
        {
            mutations = _prototypeManager
            .Index(tableId)
            .Mutations;
        }

        var options = mutations
            .Select(mutation => mutation.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .Select(name => new CompletionOption(
                $"\"{name}\"",
                Flags: CompletionOptionFlags.NoEscape));

        return CompletionResult.FromHintOptions(options, "<mutation name>");
    }
}
