using Robust.Client.Graphics;

namespace Content.Client.Light.EntitySystems;

public sealed class PlanetLightSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetClearColorEvent>(OnClearColor);

        _overlayMan.AddOverlay(new BeforeLightTargetOverlay());
        _overlayMan.AddOverlay(new RoofOverlay(EntityManager));
        _overlayMan.AddOverlay(new TileEmissionOverlay(EntityManager));
        _overlayMan.AddOverlay(new LightBlurOverlay());
        _overlayMan.AddOverlay(new SunShadowOverlay());
        _overlayMan.AddOverlay(new AfterLightTargetOverlay());
    }

    private void OnClearColor(ref GetClearColorEvent ev)
    {
        ev.Color = Color.Transparent;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<BeforeLightTargetOverlay>();
        _overlayMan.RemoveOverlay<RoofOverlay>();
        _overlayMan.RemoveOverlay<TileEmissionOverlay>();
        _overlayMan.RemoveOverlay<LightBlurOverlay>();
        _overlayMan.RemoveOverlay<SunShadowOverlay>();
        _overlayMan.RemoveOverlay<AfterLightTargetOverlay>();
    }
}
