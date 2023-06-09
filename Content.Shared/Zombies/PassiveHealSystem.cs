using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

public class PassiveHealSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveHealComponent, MobStateChangedEvent>(OnMobState);
    }

    private void OnMobState(EntityUid uid, PassiveHealComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemCompDeferred<PassiveHealComponent>(uid);
        }
    }

    public PassiveHealComponent BeginHealing(EntityUid uid, float pointsPerSec)
    {
        var heal = EnsureComp<PassiveHealComponent>(uid);
        heal.PointsPerSec = pointsPerSec;
        return heal;
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
                // Autoheal a mix of damage to achieve a health improvement of heal.PointsPerSec
                var multiplier = Math.Max(1.0f, (float) (heal.PointsPerSec / damage.TotalDamage ));
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
