using Content.Server.Atmos.EntitySystems;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;

namespace Content.Server.IgnitionSource;

/// <summary>
/// This handles ignition, Jez basically coded this.
/// </summary>
///
public sealed class IgnitionSourceSystem : EntitySystem
{
    /// <inheritdoc/>
    ///
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IgnitionSourceComponent,IsHotEvent>(OnIsHot);
    }

    private void OnIsHot(EntityUid uid, IgnitionSourceComponent component, IsHotEvent args)
    {
        Logger.Debug(args.IsHot.ToString());
        SetIgnited(uid,component,args.IsHot);
    }

    private void SetIgnited(EntityUid uid, IgnitionSourceComponent component, bool newState)
    {
        component.Ignited = newState;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (component,transform) in EntityQuery<IgnitionSourceComponent,TransformComponent>())
        {
            var source = component.Owner;
            if (!component.Ignited)
                continue;

            if (transform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(source, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, component.Temperature, 50, true);
            }
        }

    }
}
