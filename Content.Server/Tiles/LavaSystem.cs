using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Climbing;
using Content.Shared.Climbing.Events;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Tiles;

public sealed class LavaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        // My brother in christ climbing code why are you like this.
        SubscribeLocalEvent<FlammableComponent, StartClimbEvent>(OnFlammableClimb);
        SubscribeLocalEvent<FlammableComponent, EndClimbEvent>(OnFlammableNoClimb);
        SubscribeLocalEvent<OnLavaComponent, EntityUnpausedEvent>(OnLavaClimbUnpause);
        SubscribeLocalEvent<LavaDisintegrationComponent, LandEvent>(OnThrownLand);
    }

    private void OnThrownLand(EntityUid uid, LavaDisintegrationComponent component, ref LandEvent args)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = grid.LocalToTile(xform.Coordinates);
        var anchored = grid.GetAnchoredEntitiesEnumerator(tile);
        var lavaQuery = GetEntityQuery<LavaComponent>();

        while (anchored.MoveNext(out var ent))
        {
            if (!lavaQuery.TryGetComponent(ent.Value, out var lava))
                continue;

            QueueDel(uid);
            _audio.PlayPvs(lava.DisintegrationSound, ent.Value);
        }
    }

    private void OnFlammableNoClimb(EntityUid uid, FlammableComponent component, ref EndClimbEvent args)
    {
        RemCompDeferred<OnLavaComponent>(uid);
    }

    private void OnLavaClimbUnpause(EntityUid uid, OnLavaComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdate += args.PausedTime;
    }

    private void OnFlammableClimb(EntityUid uid, FlammableComponent component, StartClimbEvent args)
    {
        var comp = EnsureComp<OnLavaComponent>(uid);
        comp.NextUpdate = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var lavaQuery = GetEntityQuery<LavaComponent>();
        var flammableQuery = GetEntityQuery<FlammableComponent>();

        foreach (var (comp, xform) in EntityQuery<OnLavaComponent, TransformComponent>())
        {
            if (comp.NextUpdate > curTime)
                continue;

            comp.NextUpdate += TimeSpan.FromSeconds(1f);

            if (!flammableQuery.TryGetComponent(comp.Owner, out var flammable))
            {
                RemCompDeferred<OnLavaComponent>(comp.Owner);
                continue;
            }

            var grid = EntityManager.GetComponentOrNull<MapGridComponent>(xform.GridUid);

            if (grid == null)
                continue;

            var anchored = grid.GetAnchoredEntitiesEnumerator(grid.LocalToTile(xform.Coordinates));

            while (anchored.MoveNext(out var ent))
            {
                if (!lavaQuery.HasComponent(ent.Value))
                    continue;

                // Apply!
                _flammable.AdjustFireStacks(comp.Owner, 1f, flammable);
                _flammable.Ignite(comp.Owner, flammable);
                break;
            }
        }
    }
}
