using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IResourceCache _cahce = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override string Command => "showaccessreaders";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.RemoveOverlay<AccessOverlay>())
        {
            shell.WriteLine(Loc.GetString($"cmd-showaccessreaders-disabled"));
            return;
        }

        _overlay.AddOverlay(new AccessOverlay(_ent, _cahce, _xform));
        shell.WriteLine(Loc.GetString($"cmd-showaccessreaders-enabled"));

    }
}
