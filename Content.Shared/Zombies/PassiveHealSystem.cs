using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

/// <summary>
///   For providing a flat heal each second to a living mob.
/// </summary>
public sealed class PassiveHealSystem : EntitySystem
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
        if (args.NewMobState == component.CancelState)
        {
            RemCompDeferred<PassiveHealComponent>(uid);
        }
    }

    public void BeginHealing(EntityUid uid, DamageSpecifier healPerSec,
        MobState? cancelState = MobState.Critical, PassiveHealComponent? heal = null)
    {
        if (!Resolve(uid, ref heal))
        {
            heal = EnsureComp<PassiveHealComponent>(uid);
        }
        heal.HealPerSec = healPerSec;
        heal.CancelState = cancelState;
        heal.NextTick = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<PassiveHealComponent, DamageableComponent>();

        // Heal the mobs
        while (query.MoveNext(out var uid, out var heal, out var damage))
        {
            // Heal only once per second
            if (heal.NextTick > curTime)
                continue;

            // Don't heal again for a while.
            heal.NextTick += TimeSpan.FromSeconds(1);

            if (damage.TotalDamage > 0.01)
            {
                _damageable.TryChangeDamage(uid, heal.HealPerSec, true, false, damage);
            }
        }
    }
}
