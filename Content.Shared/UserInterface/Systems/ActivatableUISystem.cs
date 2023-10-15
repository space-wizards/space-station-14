using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.UserInterface.Events;
using Content.Shared.Verbs;
using Robust.Shared.Players;
using ActorComponent = Robust.Shared.GameObjects.ActorComponent;

namespace Content.Shared.UserInterface.Systems;

public sealed partial class ActivatableUISystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.ActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<Components.ActivatableUIComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<Components.ActivatableUIComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<Components.ActivatableUIComponent, GotUnequippedHandEvent>((uid, aui, _) => CloseAll(uid, aui));
        // *THIS IS A BLATANT WORKAROUND!* RATIONALE: Microwaves need it
        SubscribeLocalEvent<Components.ActivatableUIComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<Components.ActivatableUIComponent, BoundUIClosedEvent>(OnUIClose);
        SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);

        SubscribeLocalEvent<Components.ActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);

        SubscribeLocalEvent<UserInterfaceComponent, OpenUiActionEvent>(OnActionPerform);

        InitializePower();
    }

    private void OnBoundInterfaceInteractAttempt(BoundUserInterfaceMessageAttempt ev)
    {
        if (!TryComp(ev.Target, out Components.ActivatableUIComponent? comp))
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

        args.Handled = _uiSystem.TryToggleUi(uid, args.Key, actor.Session);
    }

    private void AddOpenUiVerb(EntityUid uid, Components.ActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
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

    private void OnActivate(EntityUid uid, Components.ActivatableUIComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (component.InHandsOnly)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnUseInHand(EntityUid uid, Components.ActivatableUIComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.rightClickOnly)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnParentChanged(EntityUid uid, Components.ActivatableUIComponent aui, ref EntParentChangedMessage args)
    {
        CloseAll(uid, aui);
    }

    private void OnUIClose(EntityUid uid, Components.ActivatableUIComponent component, BoundUIClosedEvent args)
    {
        if (args.Session != component.CurrentSingleUser)
            return;

        if (!Equals(args.UiKey, component.Key))
            return;

        SetCurrentSingleUser(uid, null, component);
    }

    private bool InteractUI(EntityUid user, EntityUid uiEntity, Components.ActivatableUIComponent aui)
    {
        if (!_blockerSystem.CanInteract(user, uiEntity) && (!aui.AllowSpectator || !HasComp<GhostComponent>(user)))
            return false;

        if (aui.RequireHands && !HasComp<HandsComponent>(user))
            return false;

        if (!EntityManager.TryGetComponent(user, out ActorComponent? actor))
            return false;

        if (aui.AdminOnly && !_adminManager.IsAdmin(actor.Session))
            return false;

        if (aui.Key == null)
            return false;

        if (!_uiSystem.TryGetUi(uiEntity, aui.Key, out var ui))
            return false;

        if (aui.SingleUser && (aui.CurrentSingleUser != null) && (actor.Session != aui.CurrentSingleUser))
        {
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

        SetCurrentSingleUser(uiEntity, actor.Session, aui);
        _uiSystem.Toggle(ui, actor.Session);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aae = new AfterActivatableUIOpenEvent(user, actor.Session);
        RaiseLocalEvent(uiEntity, ref aae);

        return true;
    }

    public void SetCurrentSingleUser(EntityUid uid, ICommonSession? v, Components.ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui))
            return;
        if (!aui.SingleUser)
            return;

        aui.CurrentSingleUser = v;

        RaiseLocalEvent(uid, new ActivatableUIPlayerChangedEvent());
    }

    public void CloseAll(EntityUid uid, Components.ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui, false))
            return;

        if (aui.Key == null || !_uiSystem.TryGetUi(uid, aui.Key, out var ui))
            return;

        _uiSystem.CloseAll(ui);
    }

    private void OnHandDeselected(EntityUid uid, Components.ActivatableUIComponent? aui, HandDeselectedEvent args)
    {
        if (!Resolve(uid, ref aui, false))
            return;

        if (!aui.CloseOnHandDeselect)
            return;

        CloseAll(uid, aui);
    }
}
