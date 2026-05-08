using Content.Client.Stylesheets.Fonts;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IFontSelectionManager _fontSelection = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override string Command => "showaccessreaders";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var existing = _overlay.RemoveOverlay<AccessOverlay>();
        if (!existing)
            _overlay.AddOverlay(new AccessOverlay(EntityManager, _fontSelection, _xform));

        shell.WriteLine(Loc.GetString($"cmd-showaccessreaders-status", ("status", !existing)));
    }
}
