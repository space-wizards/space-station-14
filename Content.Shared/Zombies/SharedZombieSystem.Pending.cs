using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

/// <summary>
///   Deals damage to bitten zombie victims each tick until they die. Then (serverside) zombifies them.
/// </summary>
public abstract partial class SharedZombieSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // Hurt them each second. Once they die, PendingZombieSystem will call Zombify and remove PendingZombieComponent
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<PendingZombieComponent, ZombieComponent, DamageableComponent, MobStateComponent>();
        // Hurt the living infected
        while (query.MoveNext(out var uid, out var pending, out var zombie, out var damage, out var mobState))
        {
            // Process only once per second
            if (pending.NextTick > curTime)
                continue;

            // Don't hurt again for a while.
            pending.NextTick = curTime + TimeSpan.FromSeconds(1);

            var infectedTime = curTime - pending.InfectionStarted;
            var infectedSecs = (int)infectedTime.TotalSeconds;
            // See if there should be a warning popup for the player.
            if (zombie.InfectionWarnings.TryGetValue(infectedSecs, out var popupStr))
            {
                _popup.PopupEntity(Loc.GetString(popupStr), uid, uid);
            }

            // If the zombie is dead, consider converting then continue to next zombie
            if (mobState.CurrentState == MobState.Dead)
            {
                // DeadMinTurnTime is enforced to give the living a moment to realize their ally is converting
                if (infectedTime > pending.DeadMinTurnTime)
                    ZombifyNow(uid, pending, zombie, mobState);  // NB: This removes PendingZombieComponent
                continue;
            }

            if (infectedTime < pending.GracePeriod && mobState.CurrentState == MobState.Alive)
            {
                // Don't hurt this zombie yet.
                continue;
            }

            var painMultiple = mobState.CurrentState == MobState.Critical
                ? pending.CritDamageMultiplier
                : 1f;

            _damageable.TryChangeDamage(uid, zombie.VirusDamage * painMultiple, true, false, damage);
        }
    }

    public void Infect(EntityUid uid, TimeSpan gracePeriod, TimeSpan minTime)
    {
        var pending = EnsureComp<PendingZombieComponent>(uid);
        pending.GracePeriod = gracePeriod;
        pending.InfectionStarted = _timing.CurTime;
        pending.DeadMinTurnTime = minTime;

        Dirty(uid, pending);
    }

    private void OnUnpause(EntityUid uid, PendingZombieComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTick += args.PausedTime;
        component.InfectionStarted += args.PausedTime;
        Dirty(uid, component);
    }

    protected virtual void ZombifyNow(EntityUid uid, PendingZombieComponent pending, ZombieComponent zombie, MobStateComponent mobState)
    {
        // Server only (see PendingZombieSystem, where the server-only ZombifyEntity is called.)
    }
}
