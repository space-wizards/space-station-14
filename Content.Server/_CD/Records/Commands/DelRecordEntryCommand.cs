using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared._CD.Records;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CD.Records.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class DelRecordEntryCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "delrecordentry";

    public string Description =>
        "Resets the records of the given entity to the default values. This is not saved to the database and only lasts until the round is over";

    public string Help => $"{Command} <entity> <recordType> <index>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine($"Not enough arguments.\n{Help}");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine($"Invalid entity id.");
            return;
        }

        if (!Enum.TryParse<CharacterRecordType>(args[1], out var ty))
        {
            shell.WriteLine($"Invalid entry type.");
            return;
        }

        if (!int.TryParse(args[2], out var idx))
        {
            shell.WriteLine($"Invalid index.");
            return;
        }

        var characterRecordsSystem = _entManager.System<CharacterRecordsSystem>();
        var stationSystem = _entManager.System<StationSystem>();

        foreach (var s in stationSystem.GetStations())
        {
            characterRecordsSystem.DelEntry(s, uid.Value, ty, idx);
        }
    }

}
