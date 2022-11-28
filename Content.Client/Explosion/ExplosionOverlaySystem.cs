using Content.Shared.Explosion;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client.Explosion;

/// <summary>
///     This system is responsible for showing the client-side explosion effects (light source & fire-overlay). The
///     fire overlay code is just a bastardized version of the atmos plasma fire overlay and uses the same texture.
/// </summary>
public sealed class ExplosionOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    /// <summary>
    ///     For how many seconds should an explosion stay on-screen once it has finished expanding?
    /// </summary>
    public float ExplosionPersistence = 0.3f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentInit>(OnExplosionInit);
        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentRemove>(OnCompRemove);
        SubscribeLocalEvent<ExplosionVisualsComponent, ComponentHandleState>(OnExplosionHandleState);
        _overlayMan.AddOverlay(new ExplosionOverlay());
    }

    private void OnExplosionHandleState(EntityUid uid, ExplosionVisualsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ExplosionVisualsState state)
            return;

        component.Epicenter = state.Epicenter;
        component.SpaceTiles = state.SpaceTiles;
        component.Tiles = state.Tiles;
        component.Intensity = state.Intensity;
        component.ExplosionType = state.ExplosionType;
        component.SpaceMatrix = state.SpaceMatrix;
        component.SpaceTileSize = state.SpaceTileSize;
    }

    private void OnCompRemove(EntityUid uid, ExplosionVisualsComponent component, ComponentRemove args)
    {
        QueueDel(component.LightEntity);
    }

    private void OnExplosionInit(EntityUid uid, ExplosionVisualsComponent component, ComponentInit args)
    {
        if (!_protoMan.TryIndex(component.ExplosionType, out ExplosionPrototype? type))
            return;

        // spawn in a client-side light source at the epicenter
        var lightEntity = Spawn("ExplosionLight", component.Epicenter);
        var light = EnsureComp<PointLightComponent>(lightEntity);
        light.Energy = light.Radius = component.Intensity.Count;
        light.Color = type.LightColor;
        component.LightEntity = lightEntity;
        component.FireColor = type.FireColor;
        component.IntensityPerState = type.IntensityPerState;

        var fireRsi = _resCache.GetResource<RSIResource>(type.TexturePath).RSI;
        foreach (var state in fireRsi)
        {
            component.FireFrames.Add(state.GetFrames(RSI.State.Direction.South));
            if (component.FireFrames.Count == type.FireStates)
                break;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<ExplosionOverlay>();
    }
}
