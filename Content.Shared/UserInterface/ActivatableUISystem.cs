using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Managers;
using Content.Shared.Ghost.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using JetBrains.Annotations;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem : EntitySystem
{
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private bool ShouldAddVerb<T>(Entity<ActivatableUIComponent> ent, GetVerbsEvent<T> args) where T : Verb
    {
        if (!args.CanAccess)
            return false;

        if (_whitelist.IsWhitelistFail(ent.Comp.RequiredItems, args.Using ?? default))
            return false;

        if (ent.Comp.RequiresComplex)
        {
            if (args.Hands == null)
                return false;

            if (!ValidateHandRequirement(ent, args.User))
                return false;
        }

        return (args.CanInteract
                || HasComp<GhostComponent>(args.User)
                && !ent.Comp.BlockSpectators)
               && RaiseCanOpenEventChecks(args.User, ent, silent: true); // silent to prevent popups or sounds when only looking at the verb
    }

    private bool InteractUI(EntityUid user, Entity<ActivatableUIComponent> ent)
    {
        if (ent.Comp.Key == null || !_ui.HasUi(ent, ent.Comp.Key))
            return false;

        if (_ui.IsUiOpen(ent.Owner, ent.Comp.Key, user))
        {
            _ui.CloseUi(ent.Owner, ent.Comp.Key, user);
            return true;
        }

        if (!_blocker.CanInteract(user, ent) && (!HasComp<GhostComponent>(user) || ent.Comp.BlockSpectators))
            return false;

        if (ent.Comp.RequiresComplex)
        {
            if (!_blocker.CanComplexInteract(user))
                return false;
        }

        if (!ValidateHandRequirement(ent, user))
            return false;

        if (ent.Comp.AdminOnly && !_admin.IsAdmin(user))
            return false;

        if (ent.Comp.SingleUser && ent.Comp.CurrentSingleUser != null && user != ent.Comp.CurrentSingleUser)
        {
            var message = Loc.GetString("machine-already-in-use", ("machine", ent));
            _popup.PopupEntity(message, ent, user);

            if (_ui.IsUiOpen(ent.Owner, ent.Comp.Key))
                return true;

            Log.Error($"Activatable UI has user without being opened? Entity: {ToPrettyString(ent)}. User: {ent.Comp.CurrentSingleUser}, Key: {ent.Comp.Key}");
        }

        // If we've gotten this far, fire a cancellable event that indicates someone is about to activate this.
        // This is so that stuff can require further conditions (like power).
        if (!RaiseCanOpenEventChecks(user, ent))
            return false;

        // Give the UI an opportunity to prepare itself if it needs to do anything
        // before opening
        var bae = new BeforeActivatableUIOpenEvent(user);
        RaiseLocalEvent(ent, bae);

        SetCurrentSingleUser(ent.AsNullable(), user);
        _ui.OpenUi(ent.Owner, ent.Comp.Key, user);

        //Let the component know a user opened it so it can do whatever it needs to do
        var aae = new AfterActivatableUIOpenEvent(user);
        RaiseLocalEvent(ent, aae);

        return true;
    }

    public void SetCurrentSingleUser(Entity<ActivatableUIComponent?> uiEnt, EntityUid? user)
    {
        if (!Resolve(uiEnt, ref uiEnt.Comp))
            return;

        if (!uiEnt.Comp.SingleUser)
            return;

        uiEnt.Comp.CurrentSingleUser = user;
        Dirty(uiEnt);

        RaiseLocalEvent(uiEnt, new ActivatableUIPlayerChangedEvent());
    }

    /// <summary>
    /// Deactivates all activatable UIs on the entity.
    /// </summary>
    /// <param name="uiEnt"> The entity owning the UIs to be closed. </param>
    [PublicAPI]
    public void CloseAll(Entity<ActivatableUIComponent?> uiEnt)
    {
        if (!Resolve(uiEnt, ref uiEnt.Comp, false))
            return;

        if (uiEnt.Comp.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(uiEnt)}");
            return;
        }

        _ui.CloseUi(uiEnt.Owner, uiEnt.Comp.Key);
    }

    private bool RaiseCanOpenEventChecks(EntityUid user, EntityUid uiEntity, bool silent = false)
    {
        // If we've gotten this far, fire a cancellable event that indicates someone is attempting to activate this UI.
        // This is so that stuff can require further conditions (like power).
        var uae = new UserOpenActivatableUIAttemptEvent(user, uiEntity, silent);
        RaiseLocalEvent(user, uae);

        if (uae.Cancelled)
            return false;

        var oae = new ActivatableUIOpenAttemptEvent(user, silent);
        RaiseLocalEvent(uiEntity, oae);

        if (oae.Cancelled)
            return false;

        return true;
    }

    private bool ValidateHandRequirement(Entity<ActivatableUIComponent> uiEnt, Entity<HandsComponent?> user)
    {
        if (uiEnt.Comp.InHandsOnly)
        {
            if (!Resolve(user, ref user.Comp, false))
                return false;

            if (!_hands.IsHolding(user, uiEnt, out var hand))
                return false;

            if (uiEnt.Comp.RequireActiveHand && user.Comp.ActiveHandId != hand)
                return false;
        }

        if (uiEnt.Comp.RequireEmptyHand)
        {
            if (!Resolve(user, ref user.Comp, false) || !_hands.TryGetEmptyHand(user, out _))
                return !uiEnt.Comp.InHandsOnly; // Technically empty if they have no hand.

            if (uiEnt.Comp.RequireActiveHand && !_hands.ActiveHandIsEmpty(user))
                return false;

            if (uiEnt.Comp.InHandsOnly && _hands.IsHolding(user, uiEnt, out _))
                return false; // Technically could have the item in your other hand.
        }

        return true;
    }
}
