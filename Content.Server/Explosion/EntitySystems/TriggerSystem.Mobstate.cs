using Content.Server.Explosion.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Implants;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeMobstate()
    {
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, SuicideEvent>(OnSuicide);

        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, ImplantRelayEvent<SuicideEvent>>(OnSuicideImplantRelay);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, ImplantRelayEvent<MobStateChangedEvent>>(OnMobStateImplantRelay);

        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, InventoryRelayedEvent<SuicideEvent>>(OnSuicideInventoryRelay);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, InventoryRelayedEvent<MobStateChangedEvent>>(OnMobStateInventoryRelay);
    }

    private void OnMobStateChanged(EntityUid uid, TriggerOnMobstateChangeComponent component, MobStateChangedEvent args)
    {
        if (!component.MobState.Contains(args.NewMobState))
            return;

        //This chains Mobstate Changed triggers with OnUseTimerTrigger if they have it
        //Very useful for things that require a mobstate change and a timer
        if (TryComp<OnUseTimerTriggerComponent>(uid, out var timerTrigger))
        {
            HandleTimerTrigger(
                uid,
                args.Origin,
                timerTrigger.Delay,
                timerTrigger.BeepInterval,
                timerTrigger.InitialBeepDelay,
                timerTrigger.BeepSound);
        }
        else
            Trigger(uid);
    }

    /// <summary>
    /// Checks if the user has any implants that prevent suicide to avoid some cheesy strategies
    /// Prevents suicide by handling the event without killing the user
    /// </summary>
    private void OnSuicide(EntityUid uid, TriggerOnMobstateChangeComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (!component.PreventSuicide)
            return;

        _popupSystem.PopupEntity(Loc.GetString("suicide-prevented"), args.Victim, args.Victim);
        args.Handled = true;
    }

    private void OnSuicideImplantRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<SuicideEvent> args)
    {
        OnSuicide(uid, component, args.Event);
    }

    private void OnMobStateImplantRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<MobStateChangedEvent> args)
    {
        OnMobStateChanged(uid, component, args.Event);
    }

    private void OnSuicideInventoryRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, InventoryRelayedEvent<SuicideEvent> args)
    {
        OnSuicide(uid, component, args.Args);
    }

    private void OnMobStateInventoryRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, InventoryRelayedEvent<MobStateChangedEvent> args)
    {
        OnMobStateChanged(uid, component, args.Args);
    }
}
