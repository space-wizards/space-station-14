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

    private void OnSuicideRelay(Entity<TriggerOnMobstateChangeComponent> ent, ref ImplantRelayEvent<SuicideEvent> args)
    {
        if (args.Event.Handled)
            return;

        if (!ent.Comp.PreventSuicide)
            return;

        _popup.PopupEntity(Loc.GetString("suicide-prevented"), args.Event.Victim, args.Event.Victim);
        args.Event.Handled = true;
    }
}
