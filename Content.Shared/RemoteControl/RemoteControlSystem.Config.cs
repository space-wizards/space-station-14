using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.RemoteControl.Components;
using Content.Shared.Verbs;

namespace Content.Shared.RemoteControl;

/// <summary>
/// System used for managing remote control: Granting temporary control of entities to other entities.
/// </summary>
public partial class RemoteControlSystem
{

    public void InitializeConfig()
    {
        SubscribeLocalEvent<RemoteControllerComponent, DamageChangedEvent>(OnTookDamage);
        SubscribeLocalEvent<RemoteControllerComponent, InteractionSuccessEvent>(OnSuccessfulInteract);

        SubscribeLocalEvent<RCRemoteComponent, DroppedEvent>(OnRemoteDropped);
    }

    private void OnTookDamage(Entity<RemoteControllerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased
            || !args.InterruptsDoAfters
            || ent.Comp.Controlled == null)
            return;

        TryStopRemoteControl(ent.Comp.Controlled.Value);
    }

    private void OnSuccessfulInteract(Entity<RemoteControllerComponent> ent, ref InteractionSuccessEvent args)
    {
        if (ent.Comp.Controlled == null)
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

        TryStopRemoteControl(ent.Comp.BoundTo.Value);
    }


}
