using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.AlertLevel;
using Content.Shared.Station;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class SetAlertLevelCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly SharedStationSystem _stationSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "setalertlevel";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<AlertLevelPrototype>(),
            Loc.GetString("cmd-setalertlevel-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.Booleans,
                LocalizationManager.GetString("cmd-setalertlevel-hint-2")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null)
        {
            shell.WriteLine(LocalizationManager.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (args.Length < 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        bool? locked = null;
        if (args.Length > 1)
        {
            if (bool.TryParse(args[1], out var parsedBool))
            {
                locked = parsedBool;
            }
            else
            {
                shell.WriteLine(LocalizationManager.GetString("shell-argument-must-be-boolean"));
                return;
            }
        }

        var stationUid = _stationSystem.GetOwningStation(player.AttachedEntity.Value);
        if (stationUid == null)
        {
            shell.WriteLine(LocalizationManager.GetString("cmd-setalertlevel-invalid-grid"));
            return;
        }

        var level = args[0];
        if (!_prototype.HasIndex<AlertLevelPrototype>(level))
        {
            shell.WriteLine(LocalizationManager.GetString("cmd-setalertlevel-invalid-level"));
            return;
        }

        _alertLevelSystem.SetLevel(stationUid.Value, level,
            force: true,
            locked: locked);
    }
}
