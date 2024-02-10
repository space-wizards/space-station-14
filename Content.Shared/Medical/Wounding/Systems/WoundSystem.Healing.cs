using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{
    private void InitHealing()
    {
    }

    private void HealingUpdate(float frameTime)
    {
        var woundableQuery = EntityManager.EntityQueryEnumerator<WoundableComponent, HealableComponent>();
        var woundQuery = EntityManager.EntityQueryEnumerator<WoundComponent, HealableComponent>();
        while (woundableQuery.MoveNext(out var entity, out var woundable, out var healable))
        {
            if (healable.NextUpdate > _gameTiming.CurTime)
                continue;
            TickWoundableHealing(entity, woundable, healable);
            healable.NextUpdate = _gameTiming.CurTime + _healingUpdateRate;
            Dirty(entity, healable);
        }

        while (woundQuery.MoveNext(out var entity, out var wound, out var healable))
        {
            if (healable.NextUpdate > _gameTiming.CurTime)
                continue;
            TickWoundHealing(entity, wound, healable);
            healable.NextUpdate = _gameTiming.CurTime + _healingUpdateRate;
            Dirty(entity, healable);
        }

    }

    private void TickWoundableHealing(EntityUid entity, WoundableComponent woundable, HealableComponent healable)
    {
        if (healable.Modifier <= 0)
            return;
        var evWoundable = new Entity<WoundableComponent, HealableComponent>(entity, woundable, healable);
        var attemptEv = new WoundableHealAttemptEvent(evWoundable);
        RaiseLocalEvent(entity,ref attemptEv);
        if (attemptEv.Canceled)
            return;
        var oldHealth = woundable.Health;
        woundable.Health += woundable.Health * (woundable.HealPercentage / 100 * healable.Modifier);
        var ev = new WoundableHealUpdateEvent(evWoundable, oldHealth);
        RaiseLocalEvent(entity,ref ev);
        //Clamp after raising event in case subscriber modifies health
        if (!ClampWoundableValues(entity, woundable))
            return;
        Log.Error($"{ToPrettyString(entity)} ran a heal update on a woundable with 0 or less integrity, " +
                  $"this should never happen! Make sure that gibbing check occurs before healing in this case!");
    }

    private void TickWoundHealing(EntityUid entity, WoundComponent wound, HealableComponent healable)
    {
        if (healable.Modifier <= 0)
            return;
        var evWound = new Entity<WoundComponent, HealableComponent>(entity, wound, healable);
        var attemptEv = new WoundHealAttemptEvent(evWound);
        RaiseLocalEvent(entity,ref attemptEv);
        if (attemptEv.Canceled)
            return;
        var oldSeverity = wound.Severity;
        wound.Severity += wound.Severity - wound.Severity * healable.Modifier;
        var ev = new WoundHealUpdateEvent(evWound, oldSeverity);
        RaiseLocalEvent(entity,ref ev);
    }
}
