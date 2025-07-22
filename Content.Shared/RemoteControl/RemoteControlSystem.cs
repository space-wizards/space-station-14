using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.RemoteControl.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;

namespace Content.Shared.RemoteControl;

/// <summary>
/// System used for managing remote control: Granting temporary control of entities to other entities.
/// </summary>
public sealed partial class RemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeConfig();

        SubscribeLocalEvent<RemotelyControllableComponent, MapInitEvent>(OnControllableInit);
        SubscribeLocalEvent<RemotelyControllableComponent, ComponentShutdown>(OnControllableShutdown);
        SubscribeLocalEvent<RemotelyControllableComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RemotelyControllableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<RemotelyControllableComponent, RemoteControlReturnToBodyEvent>(OnReturnToBody);
        SubscribeLocalEvent<RemotelyControllableComponent, MindUnvisitedMessage>(OnMindUnvisited);

        SubscribeLocalEvent<RCRemoteComponent, GetVerbsEvent<ActivationVerb>>(OnRCRemoteVerbs);
        SubscribeLocalEvent<RCRemoteComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RCRemoteComponent, ComponentShutdown>(OnRemoteShutdown);

        SubscribeLocalEvent<RemoteControllerComponent, MindRemovedMessage>(OnMindGotRemoved);
        SubscribeLocalEvent<RemoteControllerComponent, ComponentShutdown>(OnControllerShutdown);
    }

    private void OnExamine(Entity<RemotelyControllableComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineMessage == null
            || !args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(ent.Comp.ExamineMessage, ("user", ent.Owner)));
    }

    private void OnControllableInit(Entity<RemotelyControllableComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ReturnActionEntity, ent.Comp.ReturnActionPrototype);
    }

    private void OnControllableShutdown(Entity<RemotelyControllableComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ReturnActionEntity);

        // Ensure other linked components get cleared correctly to prevent PVS errors.
        if (TryComp<RemoteControllerComponent>(ent.Comp.Controller, out var controller))
        {
            controller.Controlled = null;
            Dirty(ent.Comp.Controller.Value, controller);
        }

        if (TryComp<RCRemoteComponent>(ent.Comp.BoundRemote, out var remote))
        {
            remote.BoundTo = null;
            DirtyField(ent.Comp.BoundRemote.Value, remote, nameof(RCRemoteComponent.BoundTo));
        }
    }

    private void OnMindUnvisited(Entity<RemotelyControllableComponent> ent, ref MindUnvisitedMessage args)
    {
        if (ent.Comp.Controller != null)
            RemCompDeferred<RemoteControllerComponent>(ent.Comp.Controller.Value);

        ent.Comp.Controller = null;
        ent.Comp.IsControlled = false;
        ent.Comp.CurrentRcConfig = null;

        Dirty(ent);
    }

    private void OnAfterInteractUsing(Entity<RemotelyControllableComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || ent.Comp.BoundRemote != null
            || !TryComp<RCRemoteComponent>(args.Used, out var remoteComp)
            || !HasComp<RemotelyControllableComponent>(args.Target))
            return;

        remoteComp.BoundTo = args.Target;
        ent.Comp.BoundRemote = args.Used;

        _popup.PopupClient(Loc.GetString(remoteComp.RemoteBoundToPopup,
            ("entityName", Identity.Name(ent, EntityManager))),
            args.User,
            args.User,
            PopupType.Medium);

        Dirty(args.Used, remoteComp);
        Dirty(ent);

        args.Handled = true;
    }

    private void OnReturnToBody(Entity<RemotelyControllableComponent> ent, ref RemoteControlReturnToBodyEvent args)
    {
        TryStopRemoteControl(ent);
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
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString(ent.Comp.RemoteWipeVerb),
            Act = () =>
            {
                remotelyComp.BoundRemote = null;
                Dirty(ent.Comp.BoundTo.Value, remotelyComp);

                ent.Comp.BoundTo = null;
                _popup.PopupClient(Loc.GetString(ent.Comp.RemoteWipePopup), user, user, PopupType.Medium);

                Dirty(ent);
            }
        });
    }

    private void OnUseInHand(Entity<RCRemoteComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.BoundTo is null)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.RemoteUnboundPopup), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo, out var controllable) || !_mob.IsAlive(ent.Comp.BoundTo.Value))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.RemoteFailPopup), args.User, args.User, PopupType.Medium);
            return;
        }

        Dirty(ent);

        TryRemoteControl((ent.Comp.BoundTo.Value, controllable), args.User, ent.Comp.Config);

        args.Handled = true;
    }

    private void OnRemoteShutdown(Entity<RCRemoteComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<RemotelyControllableComponent>(ent.Comp.BoundTo, out var remoteComp))
            return;

        TryStopRemoteControl(ent.Comp.BoundTo.Value);

        remoteComp.BoundRemote = null;
        DirtyField(ent.Comp.BoundTo.Value, remoteComp, nameof(RemotelyControllableComponent.BoundRemote));
    }

    private void OnMindGotRemoved(Entity<RemoteControllerComponent> ent, ref MindRemovedMessage args)
    {
        RemCompDeferred<RemoteControllerComponent>(ent);
    }

    private void OnControllerShutdown(Entity<RemoteControllerComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<RemotelyControllableComponent>(ent.Comp.Controlled, out var remoteComp))
            return;

        remoteComp.Controller = null;
        remoteComp.IsControlled = false;
        remoteComp.CurrentRcConfig = null;

        Dirty(ent.Comp.Controlled.Value, remoteComp);
    }

    /// <summary>
    /// Attempts to give control of an entity to the controller. The target must have RemotelyControllableComponent.
    /// </summary>
    /// <param name="ent">The entity that will be remotely controlled.</param>
    /// <param name="controller">UID of the entity that is to take control of the other.</param>
    /// <param name="config">RemoteControlConfiguration to use. If null, default will be used.</param>
    /// <returns>True if control was given to the controller, otherwise False.</returns>
    [PublicAPI]
    public bool TryRemoteControl(Entity<RemotelyControllableComponent> ent, EntityUid controller, RemoteControlConfiguration? config = null)
    {
        if (ent.Comp.IsControlled)
            return false;

        // If the target already has a mind it cannot be controlled.
        // Should probably be possible in the future but I can't see a use case outside of admeme or some mind control ability.
        if (_mind.TryGetMind(ent, out _, out _))
            return false;

        EnsureComp<RemoteControllerComponent>(controller, out var remoteController);

        remoteController.Controlled = ent.Owner;

        ent.Comp.Controller = controller;
        ent.Comp.IsControlled = true;

        // If a config is given, use that
        // Otherwise, use the default
        var providedConfig = config ?? new RemoteControlConfiguration();
        ent.Comp.CurrentRcConfig = providedConfig;
        remoteController.Config = providedConfig;

        if (_mind.TryGetMind(controller, out var mindId, out var mind))
            _mind.Visit(mindId, ent.Owner, mind, false);

        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Attempts to stop remote control on an entity.
    /// </summary>
    /// <param name="uid">The entity uid of the remote control target.</param>
    /// <returns>True If remote control is stopped, otherwise False.</returns>
    [PublicAPI]
    public bool TryStopRemoteControl(EntityUid uid)
    {
        if (!TryComp<RemotelyControllableComponent>(uid, out var remoteControl) || remoteControl.IsControlled == false)
            return false;

        if (!TryComp<VisitingMindComponent>(uid, out var visitingMind) || !_mind.TryGetMind(uid, out var mindId, out var mind, visitingmind: visitingMind))
            return false;

        _mind.UnVisit(mindId, mind);

        return true;
    }
}

/// <summary>
/// Raised on the entity when using the Return To Body action granted by RemotelyControllableComponent.
/// </summary>
public sealed partial class RemoteControlReturnToBodyEvent : InstantActionEvent;
