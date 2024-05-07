using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    private readonly List<EntityUid> _toClose = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActivatableUIComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ActivatableUIComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<ActivatableUIComponent, GotUnequippedHandEvent>(OnHandUnequipped);
        SubscribeLocalEvent<ActivatableUIComponent, BoundUIClosedEvent>(OnUIClose);
        SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(GetActivationVerb);
        SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<Verb>>(GetVerb);

        // TODO ActivatableUI
        // Add UI-user component, and listen for user container changes.
        // I.e., should lose a computer UI if a player gets shut into a locker.
        SubscribeLocalEvent<ActivatableUIComponent, EntGotInsertedIntoContainerMessage>(OnGotInserted);
        SubscribeLocalEvent<ActivatableUIComponent, EntGotRemovedFromContainerMessage>(OnGotRemoved);

        SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);
        SubscribeLocalEvent<UserInterfaceComponent, OpenUiActionEvent>(OnActionPerform);

        InitializePower();
    }

    private void OnBoundInterfaceInteractAttempt(BoundUserInterfaceMessageAttempt ev)
    {
        if (!TryComp(ev.Target, out ActivatableUIComponent? comp))
            return;

        if (!comp.RequireHands)
            return;

        if (!TryComp(ev.Actor, out HandsComponent? hands) || hands.Hands.Count == 0)
            ev.Cancel();
    }

    private void OnActionPerform(EntityUid uid, UserInterfaceComponent component, OpenUiActionEvent args)
    {
        if (args.Handled || args.Key == null)
            return;

        args.Handled = _uiSystem.TryToggleUi(uid, args.Key, args.Performer);
    }


    private void GetActivationVerb(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (component.VerbOnly || !ShouldAddVerb(uid, component, args))
            return;

        args.Verbs.Add(new ActivationVerb
        {
            // TODO VERBS add "open UI" icon
            Act = () => InteractUI(args.User, uid, component),
            Text = Loc.GetString(component.VerbText)
        });
    }

    private void GetVerb(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<Verb> args)
    {
        if (!component.VerbOnly || !ShouldAddVerb(uid, component, args))
            return;

        args.Verbs.Add(new Verb
        {
            // TODO VERBS add "open UI" icon
            Act = () => InteractUI(args.User, uid, component),
            Text = Loc.GetString(component.VerbText)
        });
    }

    private bool ShouldAddVerb<T>(EntityUid uid, ActivatableUIComponent component, GetVerbsEvent<T> args) where T : Verb
    {
        if (!args.CanAccess)
            return false;

        if (!component.RequiredItems?.IsValid(args.Using ?? default, EntityManager) ?? false)
            return false;

        if (component.RequireHands)
        {
            if (args.Hands == null)
                return false;

            if (component.InHandsOnly)
            {
                if (!_hands.IsHolding(args.User, uid, out var hand, args.Hands))
                    return false;

                if (component.RequireActiveHand && args.Hands.ActiveHand != hand)
                    return false;
            }
        }

        return args.CanInteract || component.AllowSpectator && HasComp<GhostComponent>(args.User);
    }

    private void OnUseInHand(EntityUid uid, ActivatableUIComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.VerbOnly)
            return;

        if (component.RequiredItems != null)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnActivate(EntityUid uid, ActivatableUIComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (component.VerbOnly)
            return;

        if (component.RequiredItems != null)
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnInteractUsing(EntityUid uid, ActivatableUIComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (component.VerbOnly)
            return;

        if (component.RequiredItems == null)
            return;

        if (!component.RequiredItems.IsValid(args.Used, EntityManager))
            return;

        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OnUIClose(EntityUid uid, ActivatableUIComponent component, BoundUIClosedEvent args)
    {
        var user = args.Actor;

        if (user != component.CurrentSingleUser)
            return;

        if (!Equals(args.UiKey, component.Key))
            return;

        SetCurrentSingleUser(uid, null, component);
    }

    private bool InteractUI(EntityUid user, EntityUid uiEntity, ActivatableUIComponent aui)
    {
        if (aui.Key == null || !_uiSystem.HasUi(uiEntity, aui.Key))
            return false;

        if (_uiSystem.IsUiOpen(uiEntity, aui.Key, user))
        {
            _uiSystem.CloseUi(uiEntity, aui.Key, user);
            return true;
        }

        if (!_blockerSystem.CanInteract(user, uiEntity) && (!aui.AllowSpectator || !HasComp<GhostComponent>(user)))
            return false;

        if (aui.RequireHands)
        {
            if (!TryComp(user, out HandsComponent? hands))
                return false;

            if (aui.InHandsOnly)
            {
                if (!_hands.IsHolding(user, uiEntity, out var hand, hands))
                    return false;

                if (aui.RequireActiveHand && hands.ActiveHand != hand)
                    return false;
            }
        }

        if (aui.AdminOnly && !_adminManager.IsAdmin(user))
            return false;

        if (aui.SingleUser && aui.CurrentSingleUser != null && user != aui.CurrentSingleUser)
        {
            var message = Loc.GetString("machine-already-in-use", ("machine", uiEntity));
            _popupSystem.PopupEntity(message, uiEntity, user);

            if (_uiSystem.IsUiOpen(uiEntity, aui.Key))
                return true;

            Log.Error($"Activatable UI has user without being opened? Entity: {ToPrettyString(uiEntity)}. User: {aui.CurrentSingleUser}, Key: {aui.Key}");
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

        SetCurrentSingleUser(uiEntity, user, aui);
        _uiSystem.OpenUi(uiEntity, aui.Key, user);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aae = new AfterActivatableUIOpenEvent(user, user);
        RaiseLocalEvent(uiEntity, aae);

        return true;
    }

    public void SetCurrentSingleUser(EntityUid uid, EntityUid? user, ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui))
            return;

        if (!aui.SingleUser)
            return;

        aui.CurrentSingleUser = user;
        Dirty(uid, aui);

        RaiseLocalEvent(uid, new ActivatableUIPlayerChangedEvent());
    }

    public void CloseAll(EntityUid uid, ActivatableUIComponent? aui = null)
    {
        if (!Resolve(uid, ref aui, false))
            return;

        if (aui.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(uid)}");
            return;
        }

        _uiSystem.CloseUi(uid, aui.Key);
    }

    private void OnHandDeselected(Entity<ActivatableUIComponent> ent, ref HandDeselectedEvent args)
    {
        if (ent.Comp.RequireHands && ent.Comp.InHandsOnly && ent.Comp.RequireActiveHand)
            CloseAll(ent, ent);
    }

    private void OnHandUnequipped(Entity<ActivatableUIComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (ent.Comp.RequireHands && ent.Comp.InHandsOnly)
            CloseAll(ent, ent);
    }

    private void OnGotInserted(Entity<ActivatableUIComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        CheckAccess((ent, ent));
    }

    private void OnGotRemoved(Entity<ActivatableUIComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        CheckAccess((ent, ent));
    }

    public void CheckAccess(Entity<ActivatableUIComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(ent)}");
            return;
        }

        foreach (var user in _uiSystem.GetActors(ent.Owner, ent.Comp.Key))
        {
            if (!_container.IsInSameOrParentContainer(user, ent)
                && !_interaction.CanAccessViaStorage(user, ent))
            {
                _toClose.Add(user);
                continue;

            }

            if (!_interaction.InRangeUnobstructed(user, ent))
                _toClose.Add(user);
        }

        foreach (var user in _toClose)
        {
            _uiSystem.CloseUi(ent.Owner, ent.Comp.Key, user);
        }

        _toClose.Clear();
    }
}
