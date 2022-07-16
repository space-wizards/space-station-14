using Content.Client.Parallax.Managers;
using Content.Shared.Parallax;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Parallax;

public sealed class ParallaxSystem : SharedParallaxSystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IParallaxManager _parallax = default!;

    private const string Fallback = "Default";

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ParallaxOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ParallaxOverlay>();
    }

    public ParallaxLayerPrepared[] GetParallaxLayers(MapId mapId)
    {
        return _parallax.GetParallaxLayers(GetParallax(_map.GetMapEntityId(mapId)));
    }

    public string GetParallax(MapId mapId)
    {
        return GetParallax(_map.GetMapEntityId(mapId));
    }

    public string GetParallax(EntityUid mapUid)
    {
        return TryComp<ParallaxComponent>(mapUid, out var parallax) ? parallax.Parallax : Fallback;
    }
}
