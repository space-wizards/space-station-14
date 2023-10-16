using Content.Server.Atmos.EntitySystems;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;

namespace Content.Server.IgnitionSource;

/// <summary>
/// This handles ignition, Jez basically coded this.
/// </summary>
public sealed class IgnitionSourceSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgnitionSourceComponent, IsHotEvent>(OnIsHot);
    }

    private void OnIsHot(EntityUid uid, IgnitionSourceComponent component, IsHotEvent args)
    {
        SetIgnited(uid, args.IsHot, component);
    }

    /// <summary>
    /// Simply sets the ignited field to the ignited param.
    /// </summary>
    public void SetIgnited(EntityUid uid, bool ignited = true, IgnitionSourceComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Ignited = ignited;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IgnitionSourceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Ignited)
                continue;

            if (xform.GridUid is { } gridUid)
            {
                var position = _transform.GetGridOrMapTilePosition(uid, xform);
                _atmosphere.HotspotExpose(gridUid, position, comp.Temperature, 50, source, true);
            }
        }
    }
}
