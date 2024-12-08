using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.RemoteControl.Components;
using Content.Shared.SSDIndicator;
using Content.Shared.Verbs;

namespace Content.Shared.RemoteControl;

/// <summary>
/// System used for managing remote control: Granting temporary control of entities to other entities.
/// </summary>
public abstract class SharedRemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemotelyControllableComponent, ComponentInit>(OnControllableInit);
        SubscribeLocalEvent<RemotelyControllableComponent, ComponentShutdown>(OnControllableShutdown);
        SubscribeLocalEvent<RemotelyControllableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RemotelyControllableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<RemotelyControllableComponent, RemoteControlReturnToBodyEvent>(OnReturnToBody);
        SubscribeLocalEvent<RemotelyControllableComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RemoteControllerComponent, DamageChangedEvent>(OnTookDamage);
        SubscribeLocalEvent<RemoteControllerComponent, InteractionSuccessEvent>(OnSuccessfulInteract);

        SubscribeLocalEvent<RCRemoteComponent, GetVerbsEvent<ActivationVerb>>(OnRCRemoteVerbs);
        SubscribeLocalEvent<RCRemoteComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RCRemoteComponent, ComponentShutdown>(OnRemoteShutdown);
        SubscribeLocalEvent<RCRemoteComponent, DroppedEvent>(OnRemoteDropped);
    }

    private void OnExamine(Entity<RemotelyControllableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineMessage == null
            || !args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString("rc-controlled-examine", ("user", ent.Owner)));
    }

    private void OnControllableInit(Entity<RemotelyControllableComponent> ent, ref ComponentInit args)
    {
        EntityUid? actionEnt = null;
        _actions.AddAction(ent.Owner, ref actionEnt, ent.Comp.ReturnActionPrototype);
        if (actionEnt != null)
            ent.Comp.ReturnAction = actionEnt.Value;
    }

    private void OnControllableShutdown(Entity<RemotelyControllableComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ReturnAction);
    }

    private void OnMobStateChanged(Entity<RemotelyControllableComponent> ent, ref MobStateChangedEvent args)
    {
        TryStopRemoteControl(ent);
    }

    private void OnAfterInteractUsing(Entity<RemotelyControllableComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || !TryComp<RCRemoteComponent>(args.Used, out var remoteComp)
            || !HasComp<RemotelyControllableComponent>(args.Target)
            || ent.Comp.BoundRemote != null)
            return;

        remoteComp.BoundTo = args.Target;
        ent.Comp.BoundRemote = args.Used;

        _popup.PopupClient(Loc.GetString("rc-remote-bound", ("entityName", Prototype(ent.Owner)?.Name ?? "Unknown")), args.User, args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnReturnToBody(Entity<RemotelyControllableComponent> ent, ref RemoteControlReturnToBodyEvent args)
    {
        TryStopRemoteControl(ent);
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

        _popup.PopupEntity(Loc.GetString("rc-controller-shake"), ent.Comp.Controlled.Value, ent.Comp.Controlled.Value, PopupType.MediumCaution);
    }

    private void OnRCRemoteVerbs(Entity<RCRemoteComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (args.Hands == null
            || !args.CanAccess
            || !args.CanInteract
            || ent.Comp.BoundTo == null
            || !TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo, out var remotelyComp))
            return;

        var user = args.User;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("rc-remote-wipe-verb"),
            Act = () =>
            {
                ent.Comp.BoundTo = null;
                remotelyComp.BoundRemote = null;
                _popup.PopupClient(Loc.GetString("rc-remote-wiped"), user, user, PopupType.Medium);
            }
        };
        args.Verbs.Add(verb);
    }

    private void OnUseInHand(Entity<RCRemoteComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.BoundTo is null)
        {
            _popup.PopupClient(Loc.GetString("rc-remote-unbound"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo.Value, out var controllable))
            return;

        if (!TryComp<MobStateComponent>(ent.Comp.BoundTo, out var mobState) || mobState.CurrentState != MobState.Alive)
        {
            _popup.PopupClient(Loc.GetString("rc-remote-fail"), args.User, args.User, PopupType.Medium);
            return;
        }

        TryRemoteControl((ent.Comp.BoundTo.Value, controllable), args.User);

        args.Handled = true;
    }

    private void OnRemoteShutdown(Entity<RCRemoteComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo, out var remoteComp))
            return;

        TryStopRemoteControl(ent.Comp.BoundTo.Value);

        remoteComp.BoundRemote = null;
    }

    private void OnRemoteDropped(Entity<RCRemoteComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.BoundTo is null
            || !HasComp<RemotelyControllableComponent>(ent.Comp.BoundTo.Value))
            return;

        TryStopRemoteControl(ent.Comp.BoundTo.Value);
    }

    /// <summary>
    /// Attempts to give control of an entity to the controller. The target must have RemotelyControllableComponent.
    /// </summary>
    /// <param name="ent">The entity that will be remotely controlled.</param>
    /// <param name="controller">UID of the entity that is to take control of the other.</param>
    /// <returns>True If control was given to the controller, otherwise False.</returns>
    public bool TryRemoteControl(Entity<RemotelyControllableComponent> ent, EntityUid controller)
    {
        if (!HasComp<RemotelyControllableComponent>(ent.Owner))
            return false;

        if (TryComp<SSDIndicatorComponent>(controller, out var ssd))
        {
            ssd.Enabled = false;
        }

        EnsureComp<RemoteControllerComponent>(controller, out var remoteController);

        remoteController.Controlled = ent.Owner;

        ent.Comp.Controller = controller;
        ent.Comp.IsControlled = true;

        if (_mind.TryGetMind(controller, out var mindId, out var mind))
            _mind.Visit(mindId, ent.Owner, mind);

        return true;
    }

    /// <summary>
    /// Attempts to stop remote control on an entity.
    /// </summary>
    /// <param name="uid">The entity uid of the remote control target.</param>
    /// <returns>True If remote control is stopped, otherwise False.</returns>
    public bool TryStopRemoteControl(EntityUid uid)
    {
        if (!TryComp<RemotelyControllableComponent>(uid, out var remoteControl)
            || !HasComp<VisitingMindComponent>(uid)
            || !_mind.TryGetMind(uid, out var mindId, out var mind)
            || remoteControl.IsControlled == false)
            return false;

        if (TryComp<SSDIndicatorComponent>(remoteControl.Controller, out var ssd))
            ssd.Enabled = true;

        if(remoteControl.Controller != null)
            RemCompDeferred<RemoteControllerComponent>(remoteControl.Controller.Value);

        remoteControl.Controller = null;
        remoteControl.IsControlled = false;

        _mind.UnVisit(mindId, mind);

        return true;
    }
}

public sealed partial class RemoteControlReturnToBodyEvent : InstantActionEvent
{

}
