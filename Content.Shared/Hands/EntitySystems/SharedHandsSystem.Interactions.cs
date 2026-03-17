using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Localizations;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeInteractions()
    {
        SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);
        SubscribeAllEvent<RequestActivateInHandEvent>(HandleActivateItemInHand);
        SubscribeAllEvent<RequestHandInteractUsingEvent>(HandleInteractUsingInHand);
        SubscribeAllEvent<RequestUseInHandEvent>(HandleUseInHand);
        SubscribeAllEvent<RequestMoveHandItemEvent>(HandleMoveItemFromHand);
        SubscribeAllEvent<RequestHandAltInteractEvent>(HandleHandAltInteract);

        SubscribeLocalEvent<HandsComponent, GetUsedEntityEvent>(OnGetUsedEntity);
        SubscribeLocalEvent<HandsComponent, ExaminedEvent>(HandleExamined);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.UseItemInHand, InputCmdHandler.FromDelegate(HandleUseItem, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.AltUseItemInHand, InputCmdHandler.FromDelegate(HandleAltUseInHand, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(SwapHandsPressed, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SwapHandsReverse, InputCmdHandler.FromDelegate(SwapHandsReversePressed, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.Drop, new PointerInputCmdHandler(DropPressed))
            .Register<SharedHandsSystem>();
    }

    #region Event and Key-binding Handlers
    private void HandleAltUseInHand(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryUseItemInHand(session.AttachedEntity.Value, true);
    }

    private void HandleUseItem(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryUseItemInHand(session.AttachedEntity.Value);
    }

    private void HandleMoveItemFromHand(RequestMoveHandItemEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryMoveHeldEntityToActiveHand(args.SenderSession.AttachedEntity.Value, msg.HandName);
    }

    private void HandleUseInHand(RequestUseInHandEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryUseItemInHand(args.SenderSession.AttachedEntity.Value);
    }

    private void HandleActivateItemInHand(RequestActivateInHandEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryActivateItemInHand(args.SenderSession.AttachedEntity.Value, null, msg.HandName);
    }

    private void HandleInteractUsingInHand(RequestHandInteractUsingEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryInteractHandWithActiveHand(args.SenderSession.AttachedEntity.Value, msg.HandName);
    }

    private void HandleHandAltInteract(RequestHandAltInteractEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity != null)
            TryUseItemInHand(args.SenderSession.AttachedEntity.Value, true, handName: msg.HandName);
    }

    private void SwapHandsPressed(ICommonSession? session)
    {
        SwapHands(session, false);
    }

    private void SwapHandsReversePressed(ICommonSession? session)
    {
        SwapHands(session, true);
    }

    private void SwapHands(ICommonSession? session, bool reverse)
    {
        if (!TryComp(session?.AttachedEntity, out HandsComponent? component))
            return;

        if (!_actionBlocker.CanInteract(session.AttachedEntity.Value, null))
            return;

        if (component.ActiveHandId == null || component.Hands.Count < 2)
            return;

        var currentIndex = component.SortedHands.IndexOf(component.ActiveHandId);
        var newActiveIndex = (currentIndex + (reverse ? -1 : 1) + component.Hands.Count) % component.Hands.Count;
        var nextHand = component.SortedHands[newActiveIndex];

        TrySetActiveHand((session.AttachedEntity.Value, component), nextHand);
    }

    private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid netEntity)
    {
        if (TryComp(session?.AttachedEntity, out HandsComponent? hands) && hands.ActiveHandId != null)
            TryDrop((session.AttachedEntity.Value, hands), hands.ActiveHandId, coords);

        // always send to server.
        return false;
    }
    #endregion

    public bool TryActivateItemInHand(EntityUid uid, HandsComponent? handsComp = null, string? handName = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        var hand = handName;
        if (!TryGetHand(uid, hand, out _))
            hand = handsComp.ActiveHandId;

        if (!TryGetHeldItem((uid, handsComp), hand, out var held))
            return false;

        return _interactionSystem.InteractionActivate(uid, held.Value);
    }

    public bool TryInteractHandWithActiveHand(EntityUid uid, string handName, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!TryGetActiveItem((uid, handsComp), out var activeHeldItem))
            return false;

        if (!TryGetHeldItem((uid, handsComp), handName, out var held))
            return false;

        _interactionSystem.InteractUsing(uid, activeHeldItem.Value, held.Value, Transform(held.Value).Coordinates);
        return true;
    }

    public bool TryUseItemInHand(EntityUid uid, bool altInteract = false, HandsComponent? handsComp = null, string? handName = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        var hand = handName;
        if (!TryGetHand(uid, hand, out _))
            hand = handsComp.ActiveHandId;

        if (!TryGetHeldItem((uid, handsComp), hand, out var held))
            return false;

        if (altInteract)
            return _interactionSystem.AltInteract(uid, held.Value);
        return _interactionSystem.UseInHandInteraction(uid, held.Value);
    }

    /// <summary>
    ///     Moves an entity from one hand to the active hand.
    /// </summary>
    public bool TryMoveHeldEntityToActiveHand(EntityUid uid, string handName, bool checkActionBlocker = true, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (handsComp.ActiveHandId == null || !HandIsEmpty((uid, handsComp), handsComp.ActiveHandId))
            return false;

        if (!TryGetHeldItem((uid, handsComp), handName, out var entity))
            return false;

        if (!CanDropHeld(uid, handName, checkActionBlocker))
            return false;

        if (!CanPickupToHand(uid, entity.Value, handsComp.ActiveHandId, checkActionBlocker: checkActionBlocker, handsComp: handsComp))
            return false;

        DoDrop(uid, handName, false, log: false);
        DoPickup(uid, handsComp.ActiveHandId, entity.Value, handsComp, log: false);
        return true;
    }

    private void OnGetUsedEntity(EntityUid uid, HandsComponent component, ref GetUsedEntityEvent args)
    {
        if (args.Handled)
            return;

        if (TryGetActiveItem((uid, component), out var activeHeldItem))
        {
            // allow for the item to return a different entity, e.g. virtual items
            RaiseLocalEvent(activeHeldItem.Value, ref args);
        }

        args.Used ??= activeHeldItem;
    }

    //TODO: Actually shows all items/clothing/etc.
    private void HandleExamined(EntityUid examinedUid, HandsComponent handsComp, ExaminedEvent args)
    {
        var heldItemNames = EnumerateHeld((examinedUid, handsComp))
            .Where(entity => !HasComp<VirtualItemComponent>(entity))
            .Select(item => FormattedMessage.EscapeText(Identity.Name(item, EntityManager)))
            .Select(itemName => Loc.GetString("comp-hands-examine-wrapper", ("item", itemName)))
            .ToList();

        var locKey = heldItemNames.Count != 0 ? "comp-hands-examine" : "comp-hands-examine-empty";
        var locUser = ("user", Identity.Entity(examinedUid, EntityManager));
        var locItems = ("items", ContentLocalizationManager.FormatList(heldItemNames));

        using (args.PushGroup(nameof(HandsComponent)))
        {
            args.PushMarkup(Loc.GetString(locKey, locUser, locItems));
        }
    }
}
