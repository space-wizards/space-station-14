using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Player;

namespace Content.Shared.StatusEffectNew;

public sealed partial class StatusEffectsSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<StatusEffectContainerComponent, LocalPlayerAttachedEvent>(RelayStatusEffectEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, LocalPlayerDetachedEvent>(RelayStatusEffectEvent);
        // SubscribeLocalEvent<StatusEffectContainerComponent, RejuvenateEvent>(RelayStatusEffectEvent); // Offbrand

        SubscribeLocalEvent<StatusEffectContainerComponent, RefreshMovementSpeedModifiersEvent>(RelayStatusEffectEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, UpdateCanMoveEvent>(RelayStatusEffectEvent);

        SubscribeLocalEvent<StatusEffectContainerComponent, RefreshFrictionModifiersEvent>(RefRelayStatusEffectEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, TileFrictionEvent>(RefRelayStatusEffectEvent);

        SubscribeLocalEvent<StatusEffectContainerComponent, StandUpAttemptEvent>(RefRelayStatusEffectEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, StunEndAttemptEvent>(RefRelayStatusEffectEvent);

        SubscribeLocalEvent<StatusEffectContainerComponent, AccentGetEvent>(RelayStatusEffectEvent);

        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.WoundGetDamageEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetWoundsWithSpaceEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetPainEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetStrainEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.HealWoundsEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetBleedLevelEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.PainSuppressionEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.BeforeDealBrainDamage>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.BeforeDepleteBrainOxygen>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.BeforeHealBrainDamage>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetOxygenationModifier>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Wounds.GetStoppedCirculationModifier>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Weapons.RelayedGetMeleeDamageEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Weapons.RelayedGetMeleeAttackRateEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared._Offbrand.Weapons.RelayedGunRefreshModifiersEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Damage.ModifySlowOnDamageSpeedEvent>(RefRelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Eye.Blinding.Systems.GetBlurEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Eye.Blinding.Systems.CanSeeAttemptEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.IdentityManagement.Components.SeeIdentityAttemptEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Movement.Pulling.Events.PullStartedMessage>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Movement.Pulling.Events.PullStoppedMessage>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Weapons.Ranged.Events.SelfBeforeGunShotEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Chemistry.Hypospray.Events.SelfBeforeHyposprayInjectsEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Damage.DamageChangedEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Examine.ExaminedEvent>(RelayStatusEffectEvent); // Offbrand
        SubscribeLocalEvent<StatusEffectContainerComponent, Content.Shared.Verbs.GetVerbsEvent<Content.Shared.Verbs.AlternativeVerb>>(RelayStatusEffectEvent); // Offbrand
    }

    private void RefRelayStatusEffectEvent<T>(EntityUid uid, StatusEffectContainerComponent component, ref T args) where T : struct
    {
        RelayEvent((uid, component), ref args);
    }

    private void RelayStatusEffectEvent<T>(EntityUid uid, StatusEffectContainerComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    public void RelayEvent<T>(Entity<StatusEffectContainerComponent> statusEffect, ref T args) where T : struct
    {
        // this copies the by-ref event if it is a struct
        var ev = new StatusEffectRelayedEvent<T>(args);
        foreach (var activeEffect in statusEffect.Comp.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(activeEffect, ref ev);
        }
        // and now we copy it back
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<StatusEffectContainerComponent> statusEffect, T args) where T : class
    {
        // this copies the by-ref event if it is a struct
        var ev = new StatusEffectRelayedEvent<T>(args);
        foreach (var activeEffect in statusEffect.Comp.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(activeEffect, ref ev);
        }
    }
}

/// <summary>
/// Event wrapper for relayed events.
/// </summary>
[ByRefEvent]
public record struct StatusEffectRelayedEvent<TEvent>(TEvent Args);
