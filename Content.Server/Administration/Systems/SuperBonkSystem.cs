using Content.Server.Administration.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Systems;

public sealed partial class SuperBonkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private ClimbSystem _climbSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TransformComponent> _transformQuery;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SuperBonkComponent> ent, ref ComponentInit args)
    {
        var (_, component) = ent;

        component.NextBonk = _timing.CurTime + component.BonkCooldown;
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

    /// <summary>
    /// Begin a grand journey to bonk every table.
    /// </summary>
    [PublicAPI]
    public void StartSuperBonk(EntityUid target, bool stopWhenDead = false)
    {
        //The other check in the code to stop when the target dies does not work if the target is already dead.
        if (stopWhenDead && TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == MobState.Dead)
            return;

        if (EnsureComp<SuperBonkComponent>(target, out var component))
            return;

        var tables = EntityQueryEnumerator<BonkableComponent>();
        var bonks = new List<EntityUid>();
        // This is done so we don't crash if something like a new table is spawned.
        while (tables.MoveNext(out var uid, out var comp))
        {
            bonks.Add(uid);
        }

        component.Tables = bonks.GetEnumerator();
        component.Tables.MoveNext(); // Move off the current selection (which is nothing)
        component.StopWhenDead = stopWhenDead;
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
