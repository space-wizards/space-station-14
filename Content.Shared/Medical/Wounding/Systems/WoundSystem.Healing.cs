using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{
    private void HealingUpdate(float frameTime)
    {
        var woundableQuery = EntityManager.EntityQueryEnumerator<WoundableComponent, HealableComponent>();
        var woundQuery = EntityManager.EntityQueryEnumerator<WoundComponent, HealableComponent>();
        while (woundableQuery.MoveNext(out var entity, out var woundable, out var healable))
        {
            if (healable.NextUpdate > _gameTiming.CurTime)
                continue;
            TickWoundableHealing(new Entity<WoundableComponent, HealableComponent>(entity, woundable, healable));
            healable.NextUpdate = _gameTiming.CurTime + _healingUpdateRate;
            Dirty(entity, healable);
        }

        while (woundQuery.MoveNext(out var entity, out var wound, out var healable))
        {
            if (healable.NextUpdate > _gameTiming.CurTime)
                continue;
            TickWoundHealing(new Entity<WoundComponent, HealableComponent>(entity, wound, healable));
            healable.NextUpdate = _gameTiming.CurTime + _healingUpdateRate;
            Dirty(entity, healable);
        }

    }

    private void TickWoundableHealing(Entity<WoundableComponent, HealableComponent> woundable)
    {
        if (woundable.Comp2.Modifier <= 0)
            return;
        var attemptEv = new WoundableHealAttemptEvent(woundable);
        RaiseLocalEvent(woundable,ref attemptEv);
        if (attemptEv.Canceled)
            return;
        var oldHealth = woundable.Comp1.Health;
        woundable.Comp1.Health += woundable.Comp1.Health * (woundable.Comp1.HealPercentage / 100 * woundable.Comp2.Modifier);
        var ev = new WoundableHealUpdateEvent(woundable, oldHealth);
        RaiseLocalEvent(woundable,ref ev);
        //Clamp after raising event in case subscriber modifies health
        if (!ClampWoundableValues(new Entity<WoundableComponent?>(woundable, woundable.Comp1)))
            return;
        Log.Error($"{ToPrettyString(woundable)} ran a heal update on a woundable with 0 or less integrity, " +
                  $"this should never happen! Make sure that gibbing check occurs before healing in this case!");
    }

    private void TickWoundHealing(Entity<WoundComponent, HealableComponent> wound)
    {
        if (wound.Comp2.Modifier <= 0)
            return;
        var attemptEv = new WoundHealAttemptEvent(wound);
        RaiseLocalEvent(wound,ref attemptEv);
        if (attemptEv.Canceled)
            return;
        var oldSeverity = wound.Comp1.Severity;
        wound.Comp1.Severity += wound.Comp1.Severity - wound.Comp1.Severity * wound.Comp2.Modifier;
        var ev = new WoundHealUpdateEvent(wound, oldSeverity);
        RaiseLocalEvent(wound,ref ev);
    }
}
