using Content.Shared.Implants;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnMobstateChangeSystem : TriggerOnXSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, SuicideEvent>(OnSuicide);

        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, ImplantRelayEvent<MobStateChangedEvent>>(OnMobStateRelay);
        SubscribeLocalEvent<TriggerOnMobstateChangeComponent, ImplantRelayEvent<SuicideEvent>>(OnSuicideRelay);
    }

    private void OnMobStateChanged(EntityUid uid, TriggerOnMobstateChangeComponent component, MobStateChangedEvent args)
    {
        if (!component.MobState.Contains(args.NewMobState))
            return;

        Trigger.Trigger(uid, component.TargetMobstateEntity ? uid : args.Origin, component.KeyOut);
    }

    private void OnMobStateRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<MobStateChangedEvent> args)
    {
        if (!component.MobState.Contains(args.Event.NewMobState))
            return;

        Trigger.Trigger(uid, component.TargetMobstateEntity ? args.ImplantedEntity : args.Event.Origin, component.KeyOut);
    }

    /// <summary>
    /// Checks if the user has any implants that prevent suicide to avoid some cheesy strategies
    /// Prevents suicide by handling the attempt event and thus blocking the suicide.
    /// </summary>
    private void OnSuicide(Entity<TriggerOnMobstateChangeComponent> ent, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.PreventSuicide)
            return;

        _popup.PopupClient(Loc.GetString("suicide-prevented"), args.Victim);
        args.Handled = true;
    }

    private void OnSuicideRelay(EntityUid uid, TriggerOnMobstateChangeComponent component, ImplantRelayEvent<SuicideEvent> args)
    {
        if (args.Event.Handled)
            return;

        if (!component.PreventSuicide)
            return;

        _popup.PopupClient(Loc.GetString("suicide-prevented"), args.Event.Victim);
        args.Event.Handled = true;
    }
}
