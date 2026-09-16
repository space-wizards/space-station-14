using System.Diagnostics;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server.Station.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class StationsCommand : ToolshedCommand
{
    private ServerStationSystem? _station;
    private CargoSystem? _cargo;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        _station ??= GetSys<ServerStationSystem>();

        return _station.GetStationsSet();
    }

    [CommandImplementation("get")]
    public EntityUid Get(IInvocationContext ctx)
    {
        _station ??= GetSys<ServerStationSystem>();

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
        _station ??= GetSys<ServerStationSystem>();

        return _station.GetOwningStation(input);
    }

    [CommandImplementation("largestgrid")]
    public EntityUid? LargestGrid([PipedArgument] EntityUid input)
    {
        _station ??= GetSys<ServerStationSystem>();
        return _station.GetLargestGrid(input);
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
    public void AddGrid([PipedArgument] EntityUid input, EntityUid grid)
    {
        _station ??= GetSys<ServerStationSystem>();
        _station.AddGridToStation(input, grid);
    }

    [CommandImplementation("rmgrid")]
    public void RmGrid([PipedArgument] EntityUid input, EntityUid grid)
    {
        _station ??= GetSys<ServerStationSystem>();
        _station.RemoveGridFromStation(input, grid);
    }

    [CommandImplementation("rename")]
    public void Rename([PipedArgument] EntityUid input, string name)
    {
        _station ??= GetSys<ServerStationSystem>();
        _station.RenameStation(input, name);
    }

    [CommandImplementation("rerollBounties")]
    public void RerollBounties([PipedArgument] EntityUid input)
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
