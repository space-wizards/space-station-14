using Content.Server.Administration.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Systems;

public sealed partial class SuperBonkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private ClimbSystem _climbSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TransformComponent> _transformQuery;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SuperBonkComponent> ent, ref ComponentStartup args)
    {
        var (uid, component) = ent;

        if (component.StopWhenDead &&
            TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
        {
            RemCompDeferred<SuperBonkComponent>(uid);
            return;
        }

        component.NextBonk = _timing.CurTime + component.BonkCooldown;

        var tables = EntityQueryEnumerator<BonkableComponent>();
        var bonks = new List<EntityUid>();
        while (tables.MoveNext(out var table, out _))
        {
            bonks.Add(table);
        }

        component.Tables = bonks.GetEnumerator();
        component.Tables.MoveNext();
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<SuperBonkComponent> ent, ref MobStateChangedEvent args)
    {
        var (uid, component) = ent;

        if (component.StopWhenDead && args.NewMobState == MobState.Dead)
            RemCompDeferred<SuperBonkComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var comps = EntityQueryEnumerator<SuperBonkComponent>();

        while (comps.MoveNext(out var uid, out var comp))
        {
            if (comp.NextBonk > _timing.CurTime)
                continue;

            if (!TryBonk(uid, comp.Tables.Current) || !comp.Tables.MoveNext())
            {
                RemComp<SuperBonkComponent>(uid);
                continue;
            }

            comp.NextBonk += comp.BonkCooldown;
        }
    }

    private bool TryBonk(EntityUid uid, EntityUid tableUid)
    {
        // It would be very weird for something without a transform component to have a bonk component
        // but just in case because I don't want to crash the server.
        if (!_transformQuery.HasComp(tableUid))
            return false;

        _transformSystem.SetCoordinates(uid, Transform(tableUid).Coordinates);
        _climbSystem.Bonk(tableUid, uid);

        return true;
    }
}
