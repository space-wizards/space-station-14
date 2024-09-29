using Robust.Client.Graphics;

namespace Content.Client.Light.EntitySystems;

public sealed class PlanetLightSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private PlanetLightOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
