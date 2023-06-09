using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

public class BurstHealSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurstHealComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<BurstHealComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public BurstHealComponent QueueBurstHeal(EntityUid uid, float minSecs, float maxSecs)
    {
        var revival = EnsureComp<BurstHealComponent>(uid);
        revival.MinHealTime = minSecs;
        revival.MaxHealTime = maxSecs;
        revival.HealTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(revival.MinHealTime,
            revival.MaxHealTime));

        return revival;
    }

    private void OnDamageChanged(EntityUid uid, BurstHealComponent revival, DamageChangedEvent args)
    {
        revival.HealTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(revival.MinHealTime,
            revival.MaxHealTime));
    }

    private void OnMobState(EntityUid uid, BurstHealComponent revival, MobStateChangedEvent args)
    {
        // If the subject either returns to being alive or being dead, remove the component.
        if (args.NewMobState != MobState.Critical)
        {
            RemCompDeferred<BurstHealComponent>(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<BurstHealComponent, DamageableComponent>();

        // Heal the mobs (zombies, probably)
        while (query.MoveNext(out var uid, out var revive, out var damage))
        {
            // Wait until it is time to apply the heal.
            if (revive.HealTime > curTime)
                continue;

            if (damage.TotalDamage > 0.01)
            {
                // Autoheal a mix of damage to achieve a health improvement of heal.PointsPerSec
                var multiplier = Math.Max(revive.HealFraction, (float)((damage.TotalDamage - revive.MinDamageLeft) / damage.TotalDamage));
                _damageable.TryChangeDamage(uid, -damage.Damage * multiplier, true, false, damage);
            }

            // You'll need to add this again next time you want a revival.
            RemCompDeferred<BurstHealComponent>(uid);
        }
    }
}
