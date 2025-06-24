using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ClearAlert : LocalizedEntityCommands
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override string Command => "clearalert";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity is not { } playerEntity)
        {
            shell.WriteLine(Loc.GetString($"shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length > 1)
        {
            var target = args[1];
            if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out playerEntity))
                return;
        }

        if (!EntityManager.TryGetComponent(playerEntity, out AlertsComponent? _))
        {
            shell.WriteLine("user has no alerts component");
            return;
        }

        var alertType = args[0];
        if (!_alerts.TryGet(alertType, out var alert))
        {
            shell.WriteLine("unrecognized alertType " + alertType);
            return;
        }

        _alerts.ClearAlert(playerEntity, alert.ID);
    }
}
