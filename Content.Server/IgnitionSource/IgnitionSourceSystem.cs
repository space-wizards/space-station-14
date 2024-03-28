using Content.Server.Atmos.EntitySystems;
using Content.Shared.Item.ItemToggle.Components;
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
        SubscribeLocalEvent<ItemToggleHotComponent, ItemToggledEvent>(OnItemToggle);
    }

    private void OnIsHot(Entity<IgnitionSourceComponent> ent, ref IsHotEvent args)
    {
        SetIgnited((ent.Owner, ent.Comp), args.IsHot);
    }
    private void OnItemToggle(Entity<ItemToggleHotComponent> ent, ref ItemToggledEvent args)
    {
        if (TryComp<IgnitionSourceComponent>(ent, out var comp))
            SetIgnited((ent.Owner, comp), args.Activated);
    }

    /// <summary>
    /// Simply sets the ignited field to the ignited param.
    /// </summary>
    public void SetIgnited(Entity<IgnitionSourceComponent?> ent, bool ignited = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Ignited = ignited;
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
                _atmosphere.HotspotExpose(gridUid, position, comp.Temperature, 50, uid, true);
            }
        }
    }
}
