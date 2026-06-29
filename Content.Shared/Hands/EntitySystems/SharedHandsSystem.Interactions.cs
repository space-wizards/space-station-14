using System.Linq;
using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Localizations;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
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
            .Bind(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem))
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
        if (session?.AttachedEntity is not { } player)
            return;

        SwapHands(player, true, false);
    }

    private void SwapHandsReversePressed(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } player)
            return;

        SwapHands(player, true, true);
    }

    private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid netEntity)
    {
        if (TryComp(session?.AttachedEntity, out HandsComponent? hands) && hands.ActiveHandId != null)
            TryDrop((session.AttachedEntity.Value, hands), hands.ActiveHandId, coords);

        // always send to server.
        return false;
    }

    private bool HandleThrowItem(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
    {
        if (playerSession?.AttachedEntity is {Valid: true} player && Exists(player) && coordinates.IsValid(EntityManager))
            ThrowHeldItem(player, coordinates, predicted: true);

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

    /// <summary>
    /// Throw the player's currently held item.
    /// </summary>
    public bool ThrowHeldItem(EntityUid player, EntityCoordinates coordinates, float minDistance = 0.1f, bool predicted = false)
    {
        if (ContainerSystem.IsEntityInContainer(player) ||
            !TryComp(player, out HandsComponent? hands) ||
            !TryGetActiveItem((player, hands), out var throwEnt) ||
            !_actionBlocker.CanThrow(player, throwEnt.Value))
            return false;

        if (_timing.CurTime < hands.NextThrowTime)
            return false;
        hands.NextThrowTime = _timing.CurTime + hands.ThrowCooldown;
        Dirty(player, hands);

        if (TryComp(throwEnt, out StackComponent? stack) && stack.Count > 1 && stack.ThrowIndividually)
        {
            var splitStack = _stack.Split((throwEnt.Value, stack), 1, Comp<TransformComponent>(player).Coordinates);

            if (splitStack is not {Valid: true})
                return false;

            throwEnt = splitStack.Value;
        }

        var direction = TransformSystem.ToMapCoordinates(coordinates).Position - TransformSystem.GetWorldPosition(player);
        if (direction == Vector2.Zero)
            return true;

        var length = direction.Length();
        var distance = Math.Clamp(length, minDistance, hands.ThrowRange);
        direction *= distance / length;

        var throwSpeed = hands.BaseThrowspeed;

        // Let other systems change the thrown entity (useful for virtual items)
        // or the throw strength.
        var ev = new BeforeThrowEvent(throwEnt.Value, direction, throwSpeed, player);
        RaiseLocalEvent(player, ref ev);

        if (ev.Cancelled)
            return true;

        // This can grief the above event so we raise it afterwards
        if (IsHolding((player, hands), throwEnt, out _) && !TryDrop(player, throwEnt.Value))
            return false;

        _throwing.TryThrow(ev.ItemUid, ev.Direction, ev.ThrowSpeed, ev.PlayerUid, compensateFriction: !HasComp<LandAtCursorComponent>(ev.ItemUid), predicted: predicted);

        return true;
    }
}
