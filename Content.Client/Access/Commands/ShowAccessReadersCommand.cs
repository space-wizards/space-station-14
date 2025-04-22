using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IResourceCache _cahce = default!;

    public string Command => "showaccessreaders";

    public string Description => "Toggles showing access reader permissions on the map";
    public string Help => """
        Overlay Info:
        -Disabled | The access reader is disabled
        +Unrestricted | The access reader has no restrictions
        +Set [Index]: [Tag Name]| A tag in an access set (accessor needs all tags in the set to be allowed by the set)
        +Key [StationUid]: [StationRecordKeyId] | A StationRecordKey that is allowed
        -Tag [Tag Name] | A tag that is not allowed (takes priority over other allows)
        """;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.RemoveOverlay<AccessOverlay>())
        {
            shell.WriteLine($"Set access reader debug overlay to false");
            return;
        }

        var xform = _ent.System<SharedTransformSystem>();

        _overlay.AddOverlay(new AccessOverlay(_ent, _cahce, xform));
        shell.WriteLine($"Set access reader debug overlay to true");
    }
}
