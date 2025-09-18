using System;
using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._CD.Records.Commands;

/// <summary>
/// Resets a player's record to the default blank template.
/// </summary>
[AdminCommand(AdminFlags.Ban)]
public sealed class PurgeCharacterRecordsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "purgecharacterrecords";

    public string Description =>
        "Resets the records of the given entity to the default values. This is not saved to the database and only lasts until the round is over";

    public string Help => $"{Command} <entity>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine($"Not enough arguments.\n{Help}");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine("Invalid entity id.");
            return;
        }

        var characterRecordsSystem = _entManager.System<CharacterRecordsSystem>();
        var stationSystem = _entManager.System<StationSystem>();

        foreach (var station in stationSystem.GetStations())
        {
            characterRecordsSystem.ResetRecord(station, uid.Value);
        }
    }
}
