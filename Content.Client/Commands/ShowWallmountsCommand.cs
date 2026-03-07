using Content.Client.Wall;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

/// <summary>
/// Shows the area in which entities with <see cref="Content.Shared.Wall.WallMountComponent" /> can be interacted from.
/// </summary>
public sealed class ShowWallmountsCommand : LocalizedCommands
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override string Command => "showwallmounts";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var existing = _overlay.RemoveOverlay<WallmountDebugOverlay>();
        if (!existing)
            _overlay.AddOverlay(new WallmountDebugOverlay());

        shell.WriteLine(Loc.GetString("cmd-showwallmounts-status", ("status", !existing)));
    }
}
