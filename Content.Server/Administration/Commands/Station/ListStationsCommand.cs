using Content.Server.Station;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Administration.Commands.Station;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListStationsCommand : IConsoleCommand
{
    public string Command => "lsstations";

    public string Description => "List all active stations";

    public string Help => "lsstations";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var (id, station) in EntitySystem.Get<StationSystem>().StationInfo)
        {
            shell.WriteLine($"{id.Id, -2} | {station.Name} | {station.MapPrototype.ID}");
        }
    }
}
