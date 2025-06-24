using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Robust.Shared.Console;

namespace Content.Server.Alert.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShowAlert : LocalizedEntityCommands
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override string Command => "showalert";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity is not { } playerEntity)
        {
            shell.WriteLine(Loc.GetString($"shell-cannot-run-command-from-server"));
            return;
        }

        switch (args.Length)
        {
            case 2:
                break;
            case 3:
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out playerEntity))
                    return;
                break;
            default:
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 2), ("upper", 3)));
                return;
        }

        if (!EntityManager.TryGetComponent(playerEntity, out AlertsComponent? _))
        {
            shell.WriteLine(Loc.GetString($"shell-entity-target-lacks-component", ("componentName", nameof(AlertsComponent))));
            return;
        }

        if (!_alerts.TryGet(args[0], out var alert))
        {
            shell.WriteLine("unrecognized alertType " + args[0]);
            return;
        }
        if (!short.TryParse(args[1], out var sevint))
        {
            shell.WriteLine("invalid severity " + sevint);
            return;
        }

        short? severity1 = sevint == -1 ? null : sevint;
        _alerts.ShowAlert(playerEntity, alert.ID, severity1);
    }
}
