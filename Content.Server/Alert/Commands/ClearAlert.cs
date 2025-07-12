using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ClearAlert : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override string Command => "clearalert";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity is not { } attachedEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length > 1 && !CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, args[1], _playerManager, out attachedEntity))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (!EntityManager.HasComponent<AlertsComponent>(attachedEntity))
        {
            shell.WriteError(Loc.GetString("shell-entity-target-lacks-component", ("componentName", nameof(AlertsComponent))));
            return;
        }

        if (!_alerts.TryGet(args[0], out var alert))
        {
            shell.WriteLine(Loc.GetString("cmd-showalert-unrecognized", ("alert", args[0])));
            return;
        }

        _alerts.ClearAlert(attachedEntity, alert.ID);
    }
}
