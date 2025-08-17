using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class CameraCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "camera";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } user)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetNetId) || !_entManager.TryGetEntity(targetNetId, out var targetUid))
        {
            if (!_playerManager.TryGetSessionByUsername(args[0], out var player)
                || player.AttachedEntity == null)
            {
                shell.WriteError(Loc.GetString("cmd-camera-wrong-argument"));
                return;
            }
            targetUid = player.AttachedEntity.Value;
        }

        var ui = new AdminCameraEui(targetUid.Value);
        _eui.OpenEui(ui, user);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-camera-hint"));
        }

        return CompletionResult.Empty;
    }
}
