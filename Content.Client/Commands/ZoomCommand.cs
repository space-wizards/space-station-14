using Content.Client.Movement.Systems;
using Content.Shared.Movement.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Console;
using System.Numerics;

namespace Content.Client.Commands;

[UsedImplicitly]
public sealed class ZoomCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "zoom";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        Vector2 zoom;
        if (args.Length is not (1 or 2))
        {
            shell.WriteLine(Help);
            return;
        }

        if (!float.TryParse(args[0], out var arg0))
        {
            shell.WriteError(LocalizationManager.GetString("cmd-parse-failure-float", ("arg", args[0])));
            return;
        }

        if (arg0 > 0)
            zoom = new(arg0, arg0);
        else
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error"));
            return;
        }

        if (args.Length == 2)
        {
            if (!float.TryParse(args[1], out var arg1))
            {
                shell.WriteError(LocalizationManager.GetString("cmd-parse-failure-float", ("arg", args[1])));
                return;
            }

            if (arg1 > 0)
                zoom.Y = arg1;
            else
            {
                shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error"));
                return;
            }
        }

        var player = _playerManager.LocalSession?.AttachedEntity;

        if (_entityManager.TryGetComponent<ContentEyeComponent>(player, out var content))
        {
            _entityManager.System<ContentEyeSystem>().RequestZoom(player.Value, zoom, true, content);
            return;
        }

        _eyeManager.CurrentEye.Zoom = zoom;
    }
}
