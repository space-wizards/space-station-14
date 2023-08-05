using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

/// <summary>
///   For providing a flat heal each second to a living mob. Currently only used by zombies.
/// </summary>
public class PassiveHealSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveHealComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<PassiveHealComponent, EntityUnpausedEvent>(OnUnpause);
    }

    private void OnUnpause(EntityUid uid, PassiveHealComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTick += args.PausedTime;
    }

    private void OnMobState(EntityUid uid, PassiveHealComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemCompDeferred<PassiveHealComponent>(uid);
        }
    }

    public void BeginHealing(EntityUid uid, float flatPerSec, DamageSpecifier? healPerSec)
    {
        var heal = EnsureComp<PassiveHealComponent>(uid);
        heal.FlatPerSec = flatPerSec;
        heal.healPerSec = healPerSec;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<PassiveHealComponent, DamageableComponent>();

        // Heal the mobs (zombies, probably)
        while (query.MoveNext(out var uid, out var heal, out var damage))
        {
            // Heal only once per second
            if (heal.NextTick > curTime)
                continue;

            if (damage.TotalDamage > 0.01)
            {
                if (heal.healPerSec != null)
                {
                    // Do specific healing first
                    _damageable.TryChangeDamage(uid, heal.healPerSec, true, false, damage);
                }

                // Now apply a flat heal across all damage types
                // Autoheal a mix of damage to achieve a health improvement of heal.PointsPerSec
                var multiplier = Math.Min(1.0f, (float) (heal.FlatPerSec / damage.TotalDamage ));
                _damageable.TryChangeDamage(uid, -damage.Damage * multiplier, true, false, damage);

                // Don't heal again for a while.
                heal.NextTick += TimeSpan.FromSeconds(1);
                if (heal.NextTick < curTime)
                {
                    // If the component is far behind the current time, reinitialize it.
                    heal.NextTick = curTime + TimeSpan.FromSeconds(1);
                }
            }
        }
    }
}
