using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override string Command => "showaccessreaders";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var existing = _overlay.RemoveOverlay<AccessOverlay>();
        if (!existing)
            _overlay.AddOverlay(new AccessOverlay(EntityManager, _cache, _xform));

        shell.WriteLine(Loc.GetString($"cmd-showaccessreaders-status", ("status", !existing)));
    }
}
