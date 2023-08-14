using Content.Server.NPC.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.KillTracking;

/// <summary>
/// This handles <see cref="KillTrackerComponent"/> and recording who is damaging and killing entities.
/// </summary>
public sealed class KillTrackingSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KillTrackerComponent, DamageChangedEvent>(OnDamageChanged);
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
                component.LifetimeDamage[key] -= args.DamageDelta.Total;
            }

            return;
        }

        var source = GetKillSource(args.Origin);
        var damage = component.LifetimeDamage.GetValueOrDefault(source);
        component.LifetimeDamage[source] = damage + args.DamageDelta.Total;
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

        KillSource? killSource;
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
            if (largestSource is not KillEnvironmentSource)
            {
                // you have to do at least 50% of the damage to get the assist.
                if (_mobThreshold.TryGetIncapThreshold(uid, out var threshold) &&
                    component.LifetimeDamage[largestSource] >= threshold / 2)
                {
                    assistSource = largestSource;
                }
            }
        }

        var ev = new KillReportedEvent(uid, killSource, assistSource);
        RaiseLocalEvent(uid, ref ev, true);
    }

    private KillSource GetKillSource(EntityUid? sourceEntity)
    {
        if (TryComp<ActorComponent>(sourceEntity, out var actor))
            return new KillPlayerSource(actor.PlayerSession.UserId);
        if (HasComp<NPCComponent>(sourceEntity))
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
[ByRefEvent]
public readonly record struct KillReportedEvent(EntityUid Entity, KillSource Primary, KillSource? Assist);
