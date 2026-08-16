using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class PassiveDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;

    #region Subscriptions

    [SubscribeLocalEvent]
    private void OnPendingMapInit(EntityUid uid, PassiveDamageComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
        Dirty<PassiveDamageComponent>((uid, component));
    }

    [SubscribeLocalEvent]
    private void OnDamageTaken(EntityUid ent, PassiveDamageComponent component, DamageDealtEvent args)
    {
        if (component.IntervalHaltOnDamageTaken == TimeSpan.Zero || !args.Damage.AnyPositive())
            return;

        var proposedUpdateTime = _timing.CurTime + component.IntervalHaltOnDamageTaken;
        if (proposedUpdateTime > component.NextDamage)
        {
            component.NextDamage = proposedUpdateTime;
            Dirty<PassiveDamageComponent>((ent, component));
        }

    }

    #endregion

    // Every tick, attempt to damage entities
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Go through every entity with the component
        var query = EntityQueryEnumerator<PassiveDamageComponent, DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            // Make sure they're up for a damage tick
            if (comp.NextDamage > curTime)
                continue;

            // Set the next time they can take damage
            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);
            Dirty<PassiveDamageComponent>((uid, comp));

            // Damage them
            foreach (var allowedState in comp.AllowedStates)
            {
                if(allowedState == mobState.CurrentState)
                    _damageable.ChangeDamage((uid, damage), comp.Damage, true, false);
            }
        }
    }
}
