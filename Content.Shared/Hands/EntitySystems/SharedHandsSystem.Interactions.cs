using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void SwapHandsPressed(ICommonSession? session)
    {
        if (!TryComp(session?.AttachedEntity, out SharedHandsComponent? component))
            return;

        if (component.ActiveHand == null || component.Hands.Count < 2)
            return;

        var newActiveIndex = component.SortedHands.IndexOf(component.ActiveHand.Name) + 1;
        var nextHand = component.SortedHands[newActiveIndex % component.Hands.Count];

        TrySetActiveHand(component.Owner, nextHand, component);
    }

    private bool DropPressed(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (TryComp(session?.AttachedEntity, out SharedHandsComponent? hands) && hands.ActiveHand != null)
            TryDrop(session!.AttachedEntity!.Value, hands.ActiveHand, coords, hands: hands);

        // always send to server.
        return false;
    }

    public bool TryActivateItemInHand(EntityUid uid, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (handsComp.CurrentlyHeldEntity == null)
            return false;

        return _interactionSystem.InteractionActivate(uid, handsComp.CurrentlyHeldEntity.Value);
    }

    public bool TryInteractHandWithActiveHand(EntityUid uid, string handName, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (handsComp.CurrentlyHeldEntity == null)
            return false;

        if (!handsComp.Hands.TryGetValue(handName, out var hand))
            return false;

        if (hand.HeldEntity == null)
            return false;

        _interactionSystem.InteractUsing(uid, handsComp.CurrentlyHeldEntity.Value, hand.HeldEntity.Value, Transform(hand.HeldEntity.Value).Coordinates);
        return true;
    }

    public bool TryUseItemInHand(EntityUid uid, bool altInteract = false, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (handsComp.CurrentlyHeldEntity == null)
            return false;

        if (altInteract)
            return _interactionSystem.AltInteract(uid, handsComp.CurrentlyHeldEntity.Value);
        else
            return _interactionSystem.UseInHandInteraction(uid, handsComp.CurrentlyHeldEntity.Value);
    }

    /// <summary>
    ///     Moves an entity from one hand to the active hand.
    /// </summary>
    public bool TryMoveHeldEntityToActiveHand(EntityUid uid, string handName, bool checkActionBlocker = true, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return false;

        if (handsComp.ActiveHand == null || !handsComp.ActiveHand.IsEmpty)
            return false;

        if (!handsComp.Hands.TryGetValue(handName, out var hand))
            return false;

        if (!CanDrop(uid, hand, checkActionBlocker, handsComp))
            return false;

        var entity = hand.HeldEntity!.Value;

        if (!CanPickup(uid, entity, handsComp.ActiveHand, checkActionBlocker, handsComp))
            return false;

        DoDrop(uid, hand, handsComp);
        DoPickup(uid, handsComp.ActiveHand, entity, handsComp);
        return true;
    }
}
