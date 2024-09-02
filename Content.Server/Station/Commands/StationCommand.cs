using System.Diagnostics;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Content.Server.Station.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class StationsCommand : ToolshedCommand
{
    private StationSystem? _station;
    private CargoSystem? _cargo;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        _station ??= GetSys<StationSystem>();

        return _station.GetStationsSet();
    }

    [CommandImplementation("get")]
    public EntityUid Get([CommandInvocationContext] IInvocationContext ctx)
    {
        _station ??= GetSys<StationSystem>();

        var set = _station.GetStationsSet();
        if (set.Count > 1 || set.Count == 0)
            ctx.ReportError(new OnlyOneStationsError());

        return set.FirstOrDefault();
    }

    [CommandImplementation("getowningstation")]
    public IEnumerable<EntityUid?> GetOwningStation([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(GetOwningStation);

    [CommandImplementation("getowningstation")]
    public EntityUid? GetOwningStation([PipedArgument] EntityUid input)
    {
        _station ??= GetSys<StationSystem>();

        return _station.GetOwningStation(input);
    }

    [CommandImplementation("largestgrid")]
    public EntityUid? LargestGrid([PipedArgument] EntityUid input)
    {
        _station ??= GetSys<StationSystem>();

        return _station.GetLargestGrid(Comp<StationDataComponent>(input));
    }

    [CommandImplementation("largestgrid")]
    public IEnumerable<EntityUid?> LargestGrid([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(LargestGrid);


    [CommandImplementation("grids")]
    public IEnumerable<EntityUid> Grids([PipedArgument] EntityUid input)
        => Comp<StationDataComponent>(input).Grids;

    [CommandImplementation("grids")]
    public IEnumerable<EntityUid> Grids([PipedArgument] IEnumerable<EntityUid> input)
        => input.SelectMany(Grids);

    [CommandImplementation("config")]
    public StationConfig? Config([PipedArgument] EntityUid input)
        => Comp<StationDataComponent>(input).StationConfig;

    [CommandImplementation("config")]
    public IEnumerable<StationConfig?> Config([PipedArgument] IEnumerable<EntityUid> input)
        => input.Select(Config);

    [CommandImplementation("addgrid")]
    public void AddGrid(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<EntityUid> grid
        )
    {
        _station ??= GetSys<StationSystem>();

        _station.AddGridToStation(input, grid.Evaluate(ctx));
    }

    [CommandImplementation("rmgrid")]
    public void RmGrid(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<EntityUid> grid
    )
    {
        _station ??= GetSys<StationSystem>();

        _station.RemoveGridFromStation(input, grid.Evaluate(ctx));
    }

    [CommandImplementation("rename")]
    public void Rename([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ValueRef<string> name
    )
    {
        _station ??= GetSys<StationSystem>();

        _station.RenameStation(input, name.Evaluate(ctx)!);
    }

    [CommandImplementation("rerollBounties")]
    public void RerollBounties([CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input)
    {
        _cargo ??= GetSys<CargoSystem>();

        _cargo.RerollBountyDatabase(input);
    }
}

public record struct OnlyOneStationsError : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow("This command doesn't function if there is more than one or no stations, explicitly specify a station with the ent command or similar.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
