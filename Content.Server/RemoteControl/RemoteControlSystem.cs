using Content.Server.Mind;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.RemoteControl;
using Content.Shared.RemoteControl.Components;
using Content.Shared.SSDIndicator;

namespace Content.Server.RemoteControl;

/// <summary>
/// guh
/// </summary>
public sealed class RemoteControlSystem : SharedRemoteControlSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemotelyControllableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<RemotelyControllableComponent, RCReturnToBodyEvent>(OnReturnToBody);
        SubscribeLocalEvent<RemotelyControllableComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RemoteControllerComponent, DamageChangedEvent>(OnTookDamage);

        SubscribeLocalEvent<RCRemoteComponent, UseInHandEvent>(OnUseInHand);

    }

    private void OnTookDamage(Entity<RemoteControllerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || !args.InterruptsDoAfters)
            return;

        if (!TryComp<VisitingMindComponent>(ent.Comp.Controlled, out var _))
            return;

        if (!_mind.TryGetMind(ent.Comp.Controlled.Value, out var mindId, out var mind))
            return;

        if (TryComp<SSDIndicatorComponent>(ent.Owner, out var SSD))
            SSD.Enabled = true;

        if(ent.Owner != null)
            RemCompDeferred<RemoteControllerComponent>(ent.Owner);

        if(TryComp<RemotelyControllableComponent>(ent.Comp.Controlled.Value, out var remotelyControlled))
            remotelyControlled.Controller = null;

        _mind.UnVisit(mindId, mind);

    }
    private void OnMobStateChanged(Entity<RemotelyControllableComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<VisitingMindComponent>(ent.Owner, out var _))
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        if (TryComp<SSDIndicatorComponent>(ent.Comp.Controller, out var SSD))
            SSD.Enabled = true;

        ent.Comp.Controller = null;
        _mind.UnVisit(mindId, mind);

    }
    private void OnAfterInteractUsing(EntityUid uid, RemotelyControllableComponent comp, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<RCRemoteComponent>(args.Used, out var remoteComp))
            return;

        if (!TryComp<RemotelyControllableComponent>(args.Target, out var _))
            return;

        if (comp.BoundRemote != null)
            return;


        remoteComp.BoundTo = args.Target;
        comp.BoundRemote = args.Used;

        _popup.PopupEntity("Bound to entity.", args.User, args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnUseInHand(Entity<RCRemoteComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.BoundTo is null)
        {
            _popup.PopupEntity("Not bound to entity.", args.User, args.User, PopupType.Medium);
            return;
        }

        if(!TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo.Value, out var controllable))
            return;

        if (!TryComp<MobStateComponent>(ent.Comp.BoundTo, out var mobState) || mobState.CurrentState != MobState.Alive)
        {
            _popup.PopupEntity("Cannot establish connection.", args.User, args.User, PopupType.Medium);
            return;
        }


        if (TryComp<SSDIndicatorComponent>(args.User, out var SSD))
        {
            SSD.Enabled = false;
        }

        EnsureComp<RemoteControllerComponent>(args.User, out var remoteController);

        remoteController.Controlled = ent.Comp.BoundTo;
        remoteController.UsedRemote = ent.Owner;

        controllable.Controller = args.User;
        controllable.IsControlled = true;

        if(_mind.TryGetMind(args.User, out var mindId, out var mind))
            _mind.Visit(mindId, ent.Comp.BoundTo.Value, mind);

        args.Handled = true;
    }

    private void OnReturnToBody(Entity<RemotelyControllableComponent> ent, ref RCReturnToBodyEvent args)
    {
        if (!TryComp<VisitingMindComponent>(ent.Owner, out var _))
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        if (TryComp<SSDIndicatorComponent>(ent.Comp.Controller, out var SSD))
            SSD.Enabled = true;

        if(ent.Comp.Controller != null)
            RemCompDeferred<RemoteControllerComponent>(ent.Comp.Controller.Value);

        ent.Comp.Controller = null;
        _mind.UnVisit(mindId, mind);

    }
}
