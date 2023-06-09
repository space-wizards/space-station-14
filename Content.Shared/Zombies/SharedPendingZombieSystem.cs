using Content.Server.Zombies;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Zombies;

public abstract class SharedPendingZombieSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
            pending.NextTick += TimeSpan.FromSeconds(1);
            if (pending.NextTick < curTime)
            {
                // If the component is far behind the current time, reinitialize it.
                pending.NextTick = curTime + TimeSpan.FromSeconds(1);
            }

            var infectedSecs = (int)(pending.InfectionStarted - curTime).TotalSeconds;

            // See if there should be a warning popup for the player.
            if (zombie.Settings.InfectionWarnings.TryGetValue(infectedSecs, out var popupStr))
            {
                _popup.PopupEntity(Loc.GetString(popupStr), uid, uid);
            }

            // If the zombie is dead, consider converting then continue to next zombie
            if (mobState.CurrentState == MobState.Dead)
            {
                // NB: This removes PendingZombieComponent
                if (infectedSecs > zombie.Settings.DeadMinTurnTime)
                    ZombifyNow(uid, pending, zombie, mobState);
                continue;
            }

            // Pain of becoming a zombie grows over time
            // By scaling the number of seconds we have an accessible way to scale this exponential function.
            //   The function was hand tuned to 120 seconds, hence the 120 constant here.
            var scaledSeconds = (120.0f / pending.MaxInfectionLength) * infectedSecs;

            // 1x at 30s, 3x at 60s, 6x at 90s, 10x at 120s. Limit at 20x so we don't gib you.
            var painMultiple = Math.Min(20f, 0.1f + 0.02f * scaledSeconds + 0.0005f * scaledSeconds * scaledSeconds);
            if (mobState.CurrentState == MobState.Critical)
            {
                // Speed up their transformation when they are (or have been) in crit by ensuring their damage
                //   multiplier is at least 10x
                painMultiple = Math.Max(pending.MinimumCritMultiplier, painMultiple);
            }
            _damageable.TryChangeDamage(uid, pending.VirusDamage * painMultiple, true, false, damage);
        }
    }

    protected virtual void ZombifyNow(EntityUid uid, PendingZombieComponent pending, ZombieComponent zombie, MobStateComponent mobState)
    {
        // Server only (see PendingZombieSystem)
    }
}
