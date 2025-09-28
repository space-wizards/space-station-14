using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.IoC;

namespace Content.Client.Overlays;

public sealed class PurpleTintOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private PurpleTintOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new PurpleTintOverlay();
        // Enable by default for now. We can later add a cvar/toggle.
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlayMan.RemoveOverlay(_overlay);
    }
}
