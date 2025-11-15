using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.RemoteControl.Components;

namespace Content.Shared.RemoteControl;

/// <summary>
/// System used for managing remote control: Granting temporary control of entities to other entities.
/// </summary>
public sealed partial class RemoteControlSystem
{
    private void InitializeConfig()
    {
        SubscribeLocalEvent<RemotelyControllableComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RemoteControllerComponent, DamageChangedEvent>(OnTookDamage);
        SubscribeLocalEvent<RemoteControllerComponent, InteractionSuccessEvent>(OnSuccessfulInteract);

        SubscribeLocalEvent<RCRemoteComponent, DroppedEvent>(OnRemoteDropped);
    }

    private void OnMobStateChanged(Entity<RemotelyControllableComponent> ent, ref MobStateChangedEvent args)
    {
        TryStopRemoteControl(ent);
    }

    private void OnTookDamage(Entity<RemoteControllerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || ent.Comp.Controlled == null || args.DamageDelta == null || args.Origin == ent.Comp.Controlled)
            return;

        if (ent.Comp.Config == null)
            return;

        var total = args.DamageDelta.GetTotal();

        if (ent.Comp.Config.NotifyOnDamaged && total >= ent.Comp.Config.NotifyDamageThreshold)
            _popup.PopupEntity(Loc.GetString("rc-controller-damaged"),
                ent.Comp.Controlled.Value,
                ent.Comp.Controlled.Value,
                PopupType.MediumCaution);

        if (ent.Comp.Config.BreakOnDamaged && total >= ent.Comp.Config.BreakDamageThreshold)
            TryStopRemoteControl(ent.Comp.Controlled.Value);
    }

    private void OnSuccessfulInteract(Entity<RemoteControllerComponent> ent, ref InteractionSuccessEvent args)
    {
        if (ent.Comp.Controlled == null || args.User == ent.Comp.Controlled)
            return;

        if (ent.Comp.Config != null && !ent.Comp.Config.NotifyOnInteract)
            return;

        _popup.PopupEntity(Loc.GetString("rc-controller-shake"),
            ent.Comp.Controlled.Value,
            ent.Comp.Controlled.Value,
            PopupType.MediumCaution);
    }

    private void OnRemoteDropped(Entity<RCRemoteComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.BoundTo is null
            || !HasComp<RemotelyControllableComponent>(ent.Comp.BoundTo))
            return;

        if (!ent.Comp.Config.BreakOnDropController)
            return;

        TryStopRemoteControl(ent.Comp.BoundTo.Value);
    }
}
