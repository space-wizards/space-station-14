using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

/// <inheritdoc cref="PoorlyAttachedComponent"/>
public abstract partial class SharedPoorlyAttachedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoorlyAttachedComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<PoorlyAttachedComponent, InventoryRelayedEvent<FellDownEvent>>(OnWearerSlipped);
        SubscribeLocalEvent<PoorlyAttachedComponent, GetVerbsEvent<AlternativeVerb>>(AddReattachVerb);
    }

    private void OnGotEquipped(Entity<PoorlyAttachedComponent> entity, ref ClothingGotEquippedEvent args)
    {
        ResetAttachmentStrength(entity.AsNullable());
    }

    private void OnWearerSlipped(Entity<PoorlyAttachedComponent> entity, ref InventoryRelayedEvent<FellDownEvent> args)
    {
        ChangeAttachmentStrength(entity.AsNullable(), entity.Comp.LossPerFall);
    }

    private void AddReattachVerb(Entity<PoorlyAttachedComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null || !args.CanInteract)
            return;

        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if (!TryComp<ClothingComponent>(entity, out var clothing) || (clothing.InSlotFlag & clothing.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((entity, null), out var container))
            return;
        var wearer = container.Owner;

        if (!entity.Comp.OthersCanReattach && args.User != wearer)
            return;

        // We don't care if the user can access the item itself (they can't if they're not the wearer).
        // We DO care if the user can access the wearer.
        if (!_interaction.InRangeAndAccessible(args.User, wearer))
            return;

        var user = args.User;
        var adjustVerb = new AlternativeVerb()
        {
            Text = Loc.GetString(entity.Comp.ReattachVerbText),
            Act = () => Reattach((entity, entity.Comp, clothing), user),
            Icon = entity.Comp.ReattachVerbIcon,
            Priority = -1,
        };
        args.Verbs.Add(adjustVerb);
    }

    /// <summary>
    /// Resecures the item to the wearer, resetting any lost attachment strength, and displays a popup.
    /// </summary>
    /// <param name="item">The item being reattached</param>
    /// <param name="user">The entity reattaching the item (may or may not be the wearer)</param>
    public void Reattach(Entity<PoorlyAttachedComponent?, ClothingComponent?> item, EntityUid user)
    {
        if (!Resolve(item, ref item.Comp1, ref item.Comp2))
            return;

        var poorlyAttachedComp = item.Comp1;
        var clothingComp = item.Comp2;

        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if ((clothingComp.InSlotFlag & clothingComp.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((item, null), out var container))
            return;
        var wearer = container.Owner;

        if (!poorlyAttachedComp.OthersCanReattach && user != wearer)
            return;

        var userIdentity = Identity.Entity(user, EntityManager);
        var wearerIdentity = Identity.Entity(wearer, EntityManager);

        if (user == wearer)
        {
            // "You reattach your item"
            var userMessage = Loc.GetString(poorlyAttachedComp.ReattachSelfPopupUser, ("entity", item.Owner));
            // "Urist McHands reattaches his item"
            var othersMessage = poorlyAttachedComp.ReattachSilentToOthers
                ? null
                : Loc.GetString(poorlyAttachedComp.ReattachSelfPopupOthers, ("entity", item.Owner), ("user", userIdentity));
            Popup.PopupPredicted(userMessage, othersMessage, wearer, user);
        }
        else
        {
            // "You reattach Urist McWearer's item"
            var userMessage = Loc.GetString(poorlyAttachedComp.ReattachOtherPopupUser, ("entity", item.Owner), ("wearer", wearerIdentity));
            // "Urist McHands reattaches your item"
            var wearerMessage = Loc.GetString(poorlyAttachedComp.ReattachOtherPopupWearer, ("entity", item.Owner), ("user", userIdentity));
            // "Urist McHands reattaches Urist McWearer's item"
            var othersMessage = poorlyAttachedComp.ReattachSilentToOthers
                ? null
                : Loc.GetString(poorlyAttachedComp.ReattachOtherPopupOthers, ("entity", item.Owner), ("user", userIdentity), ("wearer", wearerIdentity));
            var othersFilter = Filter.PvsExcept(wearer, entityManager: EntityManager).RemovePlayerByAttachedEntity(user);
            Popup.PopupClient(userMessage, wearer, user);
            Popup.PopupEntity(wearerMessage, wearer, wearer);
            Popup.PopupEntity(othersMessage, wearer, othersFilter, true);
        }

        ResetAttachmentStrength((item, poorlyAttachedComp));
    }

    /// <summary>
    /// Returns the current attachment strength of the item, from 1.0 (fully attached) to 0 (completely loose).
    /// </summary>
    /// <remarks>
    /// If the entity doesn't have a <see cref="PoorlyAttachedComponent"/>,
    /// returns 1, indicating full strength.
    /// </remarks>
    public float GetAttachmentStrength(Entity<PoorlyAttachedComponent?> item)
    {
        // If it doesn't have the component, consider it fully attached
        if (!Resolve(item, ref item.Comp, logMissing: false))
            return 1;

        var timeSinceAttached = _timing.CurTime - item.Comp.AttachmentTime;

        // Start with full strength (1), then subtract the total of all events
        return MathF.Max(0, 1f - item.Comp.EventStrengthTotal - item.Comp.LossPerSecond * (float)timeSinceAttached.TotalSeconds);
    }

    /// <summary>
    /// Adjusts the item's attachment strength by a specified amount.
    /// This will cause the item to fall off if attachment strength reaches 0, unless <paramref name="canDetach"/> is false.
    /// Used to loosen the item in response to a specific event, and should not be used for continuous change.
    /// </summary>
    /// <remarks>
    /// Has no effect but does not error if the item does not have a <see cref="PoorlyAttachedComponent"/>.
    /// </remarks>
    /// <param name="item">The target item</param>
    /// <param name="amount">How much to change attachment strength</param>
    /// <param name="canDetach">Will the item fall off if this causes attachment strength to reach 0?</param>
    public void ChangeAttachmentStrength(Entity<PoorlyAttachedComponent?> item, float amount, bool canDetach = true)
    {
        if (!Resolve(item, ref item.Comp, logMissing: false))
            return;

        item.Comp.EventStrengthTotal += amount;

        if (canDetach && GetAttachmentStrength(item) <= 0)
        {
            TryThrow((item, item.Comp));
        }
    }

    /// <summary>
    /// Resets the item's attachment strength to full.
    /// </summary>
    /// <remarks>
    /// Has no effect but does not error if the item does not have a <see cref="PoorlyAttachedComponent"/>.
    /// </remarks>
    public void ResetAttachmentStrength(Entity<PoorlyAttachedComponent?> item)
    {
        if (!Resolve(item, ref item.Comp, logMissing: false))
            return;

        item.Comp.AttachmentTime = _timing.CurTime;
        item.Comp.EventStrengthTotal = 0;
        Dirty(item);
    }

    /// <summary>
    /// Makes sure that various requirement are met, then throws the item out of the wearer's inventory.
    /// </summary>
    private bool TryThrow(Entity<PoorlyAttachedComponent> item)
    {
        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if (!TryComp<ClothingComponent>(item, out var clothing) || (clothing.InSlotFlag & clothing.Slots) == SlotFlags.NONE)
            return false;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((item, null), out var container))
            return false;
        var wearer = container.Owner;

        // Make sure the item is allowed to be unequipped
        if (!_inventory.CanUnequip(wearer, container.ID, out _))
            return false;

        Throw(item, wearer);
        return true;
    }

    /// <summary>
    /// Handles actually throwing the item out of the wearer's inventory.
    /// </summary>
    /// <remarks>
    /// Virtual because this currently can only run on the server.
    /// </remarks>
    protected virtual void Throw(Entity<PoorlyAttachedComponent> item, EntityUid wearer) { }
}
