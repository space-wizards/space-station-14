using Content.Shared.Implants;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnMobstateChangeSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

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

        _trigger.Trigger(uid, component.TargetMobstateEntity ? uid : args.Origin, component.KeyOut);
    }

    private void OnMobStateImplantRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<MobStateChangedEvent> args)
    {
        OnMobStateChanged(uid, component, args.Event);
    }

    private void OnMobStateInventoryRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, InventoryRelayedEvent<MobStateChangedEvent> args)
    {
        OnMobStateChanged(uid, component, args.Args);
    }

    /// <summary>
    /// Checks if the user has any implants that prevent suicide to avoid some cheesy strategies
    /// Prevents suicide by handling the event without killing the user
    /// TODO: This doesn't seem to work at the moment as the event is never checked for being handled.
    /// </summary>
    private void OnSuicide(EntityUid uid, TriggerOnMobstateChangeComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (!component.PreventSuicide)
            return;

        _popup.PopupClient(Loc.GetString("suicide-prevented"), args.Victim);
        args.Handled = true;
    }

    private void OnSuicideImplantRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<SuicideEvent> args)
    {
        OnSuicide(uid, component, args.Event);
    }

    private void OnSuicideInventoryRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, InventoryRelayedEvent<SuicideEvent> args)
    {
        OnSuicide(uid, component, args.Args);
    }
}
