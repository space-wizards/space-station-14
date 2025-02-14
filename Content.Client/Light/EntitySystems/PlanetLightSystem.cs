using Robust.Client.Graphics;

namespace Content.Client.Light.EntitySystems;

public sealed class PlanetLightSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private RoofOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new(EntityManager);
        _overlayMan.AddOverlay(_overlay);
        _overlayMan.AddOverlay(new LightBlurOverlay());
        _overlayMan.AddOverlay(new SunShadowOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
        _overlayMan.RemoveOverlay<LightBlurOverlay>();
        _overlayMan.RemoveOverlay<SunShadowOverlay>();
    }
}
