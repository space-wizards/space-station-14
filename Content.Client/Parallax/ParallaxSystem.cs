using Content.Client.Parallax.Data;
using Content.Client.Parallax.Managers;
using Content.Shared.Parallax;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax;

public sealed class ParallaxSystem : SharedParallaxSystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IParallaxManager _parallax = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ValidatePrototypeId<ParallaxPrototype>]
    private const string Fallback = "Default";

    public const int ParallaxZIndex = 0;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ParallaxOverlay());
        _protoManager.PrototypesReloaded += OnReload;

        SubscribeLocalEvent<ParallaxComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(ParallaxPrototype)))
            return;

        _parallax.UnloadParallax(Fallback);
        _parallax.LoadDefaultParallax();

        foreach (var comp in EntityQuery<ParallaxComponent>(true))
        {
            _parallax.UnloadParallax(comp.Parallax);
            _parallax.LoadParallaxByName(comp.Parallax);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ParallaxOverlay>();
        _protoManager.PrototypesReloaded -= OnReload;
    }

    private void OnAfterAutoHandleState(EntityUid uid, ParallaxComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!_parallax.IsLoaded(component.Parallax))
        {
            _parallax.LoadParallaxByName(component.Parallax);
        }
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
