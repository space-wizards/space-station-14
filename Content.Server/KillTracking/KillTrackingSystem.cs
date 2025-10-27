using Content.Server.NPC.HTN;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;

namespace Content.Server.KillTracking;

/// <summary>
/// This handles <see cref="KillTrackerComponent"/> and recording who is damaging and killing entities.
/// </summary>
public sealed class KillTrackingSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        // Add damage to LifetimeDamage before MobStateChangedEvent gets raised
        SubscribeLocalEvent<KillTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [ typeof(MobThresholdSystem) ]);
        SubscribeLocalEvent<KillTrackerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDamageChanged(EntityUid uid, KillTrackerComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        if (!args.DamageIncreased)
        {
            foreach (var key in component.LifetimeDamage.Keys)
            {
                component.LifetimeDamage[key] -= args.DamageDelta.GetTotal();
            }

            return;
        }

        var source = GetKillSource(args.Origin);
        var damage = component.LifetimeDamage.GetValueOrDefault(source);
        component.LifetimeDamage[source] = damage + args.DamageDelta.GetTotal();
    }

    private void OnMobStateChanged(EntityUid uid, KillTrackerComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != component.KillState || args.OldMobState >= args.NewMobState)
            return;

        // impulse is the entity that did the finishing blow.
        var killImpulse = GetKillSource(args.Origin);

        // source is the kill tracker source with the most damage dealt.
        var largestSource = GetLargestSource(component.LifetimeDamage);
        largestSource ??= killImpulse;

        KillSource killSource;
        KillSource? assistSource = null;

        if (killImpulse is KillEnvironmentSource)
        {
            // if the kill was environmental, whatever did the most damage gets the kill.
            killSource = largestSource;
        }
        else if (killImpulse == largestSource)
        {
            // if the impulse and the source are the same, there's no assist
            killSource = killImpulse;
        }
        else
        {
            // the impulse gets the kill and the most damage gets the assist
            killSource = killImpulse;

            // no assist is given to environmental kills
            if (largestSource is not KillEnvironmentSource
                && component.LifetimeDamage.TryGetValue(largestSource, out var largestDamage))
            {
                var killDamage = component.LifetimeDamage.GetValueOrDefault(killSource);
                // you have to do at least twice as much damage as the killing source to get the assist.
                if (largestDamage >= killDamage / 2)
                    assistSource = largestSource;
            }
        }

        // it's a suicide if:
        // - you caused your own death
        // - the kill source was the entity that died
        // - the entity that died had an assist on themselves
        var suicide = args.Origin == uid ||
                      killSource is KillNpcSource npc && npc.NpcEnt == uid ||
                      killSource is KillPlayerSource player && player.PlayerId == CompOrNull<ActorComponent>(uid)?.PlayerSession.UserId ||
                      assistSource is KillNpcSource assistNpc && assistNpc.NpcEnt == uid ||
                      assistSource is KillPlayerSource assistPlayer && assistPlayer.PlayerId == CompOrNull<ActorComponent>(uid)?.PlayerSession.UserId;

        var ev = new KillReportedEvent(uid, killSource, assistSource, suicide);
        RaiseLocalEvent(uid, ref ev, true);
    }

    private KillSource GetKillSource(EntityUid? sourceEntity)
    {
        if (TryComp<ActorComponent>(sourceEntity, out var actor))
            return new KillPlayerSource(actor.PlayerSession.UserId);
        if (HasComp<HTNComponent>(sourceEntity))
            return new KillNpcSource(sourceEntity.Value);
        return new KillEnvironmentSource();
    }

    private KillSource? GetLargestSource(Dictionary<KillSource, FixedPoint2> lifetimeDamages)
    {
        KillSource? maxSource = null;
        var maxDamage = FixedPoint2.Zero;
        foreach (var (source, damage) in lifetimeDamages)
        {
            if (damage < maxDamage)
                continue;
            maxSource = source;
            maxDamage = damage;
        }

        return maxSource;
    }
}

/// <summary>
/// Event broadcasted and raised by-ref on an entity with <see cref="KillTrackerComponent"/> when they are killed.
/// </summary>
/// <param name="Entity">The entity that was killed</param>
/// <param name="Primary">The primary source of the kill</param>
/// <param name="Assist">A secondary source of the kill. Can be null.</param>
/// <param name="Suicide">True if the entity that was killed caused their own death.</param>
[ByRefEvent]
public readonly record struct KillReportedEvent(EntityUid Entity, KillSource Primary, KillSource? Assist, bool Suicide);
