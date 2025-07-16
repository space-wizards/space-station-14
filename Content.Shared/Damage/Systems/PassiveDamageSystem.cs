using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared.Damage;

public sealed class PassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveDamageComponent, MapInitEvent>(OnPendingMapInit);
    }

    private void OnPendingMapInit(EntityUid uid, PassiveDamageComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

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

            // Check if there are any passive damage entries.
            if (comp.PassiveDamageList.Count == 0)
                continue;

            foreach (var entry in comp.PassiveDamageList)
            {
                // If the entity has no damage entry, skip.
                if (entry.Damage == null)
                    continue;
                // If the entity is not in a valid state for this entry, skip.
                if (entry.AllowedStates == null || !entry.AllowedStates.Contains(mobState.CurrentState))
                    continue;

                // If the damage cap is disabled, or the total damage is less/equal to the cap, add to the sum.
                if (entry.DamageCap == 0 || damage.TotalDamage <= entry.DamageCap)
                    comp.DamageSum += entry.Damage;
                // If specific damage is enabled, check only the total damage for the damage types in this entry.
                else if (entry.SpecificDamageCap == true)
                {
                    // Makes and zeros out a damage specifier containing only relevant damage types, then use that as a sieve to grab only relevant damage types from the total damage.
                    DamageSpecifier damageComparingThing = new DamageSpecifier(entry.Damage);
                    damageComparingThing -= damageComparingThing;
                    damageComparingThing.ExclusiveAdd(damage.Damage);
                    if (damageComparingThing.GetTotal() <= entry.DamageCap)
                        comp.DamageSum += entry.Damage;
                    continue;
                }
            }

            // Set the next time they can take damage
            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            // Damage them
            _damageable.TryChangeDamage(uid, comp.DamageSum, true, false, damage);
            //Reset the damage sum for the next tick
            comp.DamageSum = new();
        }
    }
}
