﻿using Content.Server.Administration;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Fluids;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShowFluidsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PuddleDebugDebugOverlaySystem _puddleDebugDebugOverlaySystem = default!;

    public override string Command => "showfluids";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString($"cmd-{Command}-must-be-player"));
            return;
        }

        var enabled = _puddleDebugDebugOverlaySystem.ToggleObserver(player);

        shell.WriteLine(enabled
            ? Loc.GetString($"cmd-{Command}-enabled")
            : Loc.GetString($"cmd-{Command}-disabled"));
    }
}
