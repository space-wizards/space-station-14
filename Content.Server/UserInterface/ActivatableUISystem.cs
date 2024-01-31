using Content.Server.Administration.Managers;
using Content.Shared.ActionBlocker;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.UserInterface;

public sealed partial class ActivatableUISystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActivatableUIComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ActivatableUIComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<ActivatableUIComponent, GotUnequippedHandEvent>((uid, aui, _) => CloseAll(uid, aui));
        // *THIS IS A BLATANT WORKAROUND!* RATIONALE: Microwaves need it
        SubscribeLocalEvent<ActivatableUIComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<ActivatableUIComponent, BoundUIClosedEvent>(OnUIClose);
        SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);

        SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);

        SubscribeLocalEvent<UserInterfaceComponent, OpenUiActionEvent>(OnActionPerform);

        InitializePower();
    }

    private void OnBoundInterfaceInteractAttempt(BoundUserInterfaceMessageAttempt ev)
    {
        if (!TryComp(ev.Target, out ActivatableUIComponent? comp))
            return;

        if (!comp.RequireHands)
            return;

        if (!TryComp(ev.Sender.AttachedEntity, out HandsComponent? hands) || hands.Hands.Count == 0)
            ev.Cancel();
    }

    private void OnActionPerform(EntityUid uid, UserInterfaceComponent component, OpenUiActionEvent args)
    {
        if (args.Handled || args.Key == null)
            return;

        if (!TryComp(args.Performer, out ActorComponent? actor))
            return;

        args.Handled = _uiSystem.TryToggleUi(uid, args.Key, actor.PlayerSession);
    }

    private void AddOpenUiVerb(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (component.RequireHands && args.Hands == null)
            return;

        if (component.InHandsOnly && args.Using != uid)
            return;

        if (!args.CanInteract && (!component.AllowSpectator || !HasComp<GhostComponent>(args.User)))
            return;

        ActivationVerb verb = new();
        verb.Act = () => InteractUI(args.User, uid, component);
        verb.Text = Loc.GetString(component.VerbText);
        // TODO VERBS add "open UI" icon?
        args.Verbs.Add(verb);
    }

    private void OnActivate(EntityUid uid, ActivatableUIComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (component.InHandsOnly)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnUseInHand(EntityUid uid, ActivatableUIComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.rightClickOnly)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnParentChanged(EntityUid uid, ActivatableUIComponent aui, ref EntParentChangedMessage args)
    {
        CloseAll(uid, aui);
    }

    private void OnUIClose(EntityUid uid, ActivatableUIComponent component, BoundUIClosedEvent args)
    {
        if (args.Session != component.CurrentSingleUser)
            return;

        if (!Equals(args.UiKey, component.Key))
            return;

        SetCurrentSingleUser(uid, null, component);
    }

    private bool InteractUI(EntityUid user, EntityUid uiEntity, ActivatableUIComponent aui)
    {
        if (!TryComp(user, out ActorComponent? actor))
            return false;

        if (aui.Key == null)
            return false;

        if (!_uiSystem.TryGetUi(uiEntity, aui.Key, out var ui))
            return false;

        if (ui.SubscribedSessions.Contains(actor.PlayerSession))
        {
            _uiSystem.CloseUi(ui, actor.PlayerSession);
            return true;
        }

        if (!_blockerSystem.CanInteract(user, uiEntity) && (!aui.AllowSpectator || !HasComp<GhostComponent>(user)))
            return false;

        if (aui.RequireHands && !HasComp<HandsComponent>(user))
            return false;

        if (aui.AdminOnly && !_adminManager.IsAdmin(actor.PlayerSession))
            return false;

        if (aui.SingleUser && (aui.CurrentSingleUser != null) && (actor.PlayerSession != aui.CurrentSingleUser))
        {
            string message = Loc.GetString("machine-already-in-use", ("machine", uiEntity));
            _popupSystem.PopupEntity(message, uiEntity, user);

            // If we get here, supposedly, the object is in use.
            // Check with BUI that it's ACTUALLY in use just in case.
            // Since this could brick the object if it goes wrong.
            if (ui.SubscribedSessions.Count != 0)
                return false;
        }

        // If we've gotten this far, fire a cancellable event that indicates someone is about to activate this.
        // This is so that stuff can require further conditions (like power).
        var oae = new ActivatableUIOpenAttemptEvent(user);
        var uae = new UserOpenActivatableUIAttemptEvent(user, uiEntity);
        RaiseLocalEvent(user, uae);
        RaiseLocalEvent(uiEntity, oae);
        if (oae.Cancelled || uae.Cancelled)
            return false;

        // Give the UI an opportunity to prepare itself if it needs to do anything
        // before opening
        var bae = new BeforeActivatableUIOpenEvent(user);
        RaiseLocalEvent(uiEntity, bae);

        SetCurrentSingleUser(uiEntity, actor.PlayerSession, aui);
        _uiSystem.OpenUi(ui, actor.PlayerSession);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aae = new AfterActivatableUIOpenEvent(user, actor.PlayerSession);
        RaiseLocalEvent(uiEntity, aae);

        return true;
    }

    public void SetCurrentSingleUser(EntityUid uid, ICommonSession? v, ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui))
            return;
        if (!aui.SingleUser)
            return;

        aui.CurrentSingleUser = v;

        RaiseLocalEvent(uid, new ActivatableUIPlayerChangedEvent());
    }

    public void CloseAll(EntityUid uid, ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui, false))
            return;

        if (aui.Key == null || !_uiSystem.TryGetUi(uid, aui.Key, out var ui))
            return;

        _uiSystem.CloseAll(ui);
    }

    private void OnHandDeselected(EntityUid uid, ActivatableUIComponent? aui, HandDeselectedEvent args)
    {
        if (!Resolve(uid, ref aui, false))
            return;

        if (!aui.CloseOnHandDeselect)
            return;

        CloseAll(uid, aui);
    }
}

public sealed class ActivatableUIOpenAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public ActivatableUIOpenAttemptEvent(EntityUid who)
    {
        User = who;
    }
}

public sealed class UserOpenActivatableUIAttemptEvent : CancellableEntityEventArgs //have to one-up the already stroke-inducing name
{
    public EntityUid User { get; }
    public EntityUid Target { get; }
    public UserOpenActivatableUIAttemptEvent(EntityUid who, EntityUid target)
    {
        User = who;
        Target = target;
    }
}

public sealed class AfterActivatableUIOpenEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public readonly ICommonSession Session;

    public AfterActivatableUIOpenEvent(EntityUid who, ICommonSession session)
    {
        User = who;
        Session = session;
    }
}

/// <summary>
/// This is after it's decided the user can open the UI,
/// but before the UI actually opens.
/// Use this if you need to prepare the UI itself
/// </summary>
public sealed class BeforeActivatableUIOpenEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public BeforeActivatableUIOpenEvent(EntityUid who)
    {
        User = who;
    }
}

public sealed class ActivatableUIPlayerChangedEvent : EntityEventArgs
{
}
