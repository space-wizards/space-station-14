using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stacks;

// Partial for public API functions.
public abstract partial class SharedStackSystem
{
    #region Merge Stacks

    /// <summary>
    /// Moves as much stack count as we can from the donor to the recipient.
    /// Deletes the donor if count goes to 0.
    /// </summary>
    /// <param name="transferred">How much stack count was moved.</param>
    /// <param name="amount">Optional. Limits amount of stack count to move from the donor.</param>
    /// <returns> True if transferred is greater than 0. </returns>
    [PublicAPI]
    public bool TryMergeStacks(Entity<StackComponent?> donor,
                                Entity<StackComponent?> recipient,
                                out int transferred,
                                int? amount = null)
    {
        transferred = 0;

        if (donor == recipient)
            return false;

        if (!Resolve(recipient, ref recipient.Comp, false) || !Resolve(donor, ref donor.Comp, false))
            return false;

        if (recipient.Comp.StackTypeId != donor.Comp.StackTypeId)
            return false;

        // The most we can transfer
        transferred = Math.Min(donor.Comp.Count, GetAvailableSpace(recipient.Comp));
        if (transferred <= 0)
            return false;

        // transfer only as much as we want
        if (amount > 0)
            transferred = Math.Min(transferred, amount.Value);

        SetCount(donor, donor.Comp.Count - transferred);
        SetCount(recipient, recipient.Comp.Count + transferred);
        return true;
    }

    /// <summary>
    /// If the given item is a stack, this attempts to find a matching stack in the users hand and merge with that.
    /// </summary>
    /// <remarks>
    /// If the interaction fails to fully merge the stack, or if this is just not a stack, it will instead try
    /// to place it in the user's hand normally.
    /// </remarks>
    [PublicAPI]
    public void TryMergeToHands(Entity<StackComponent?> item, Entity<HandsComponent?> user)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return;

        if (!Resolve(item.Owner, ref item.Comp, false))
        {
            // This isn't even a stack. Just try to pickup as normal.
            Hands.PickupOrDrop(user.Owner, item.Owner, handsComp: user.Comp);
            return;
        }

        foreach (var held in Hands.EnumerateHeld(user))
        {
            TryMergeStacks(item, held, out _);

            if (item.Comp.Count == 0)
                return;
        }

        Hands.PickupOrDrop(user.Owner, item.Owner, handsComp: user.Comp);
    }

    /// <summary>
    /// Donor entity merges stack count into contacting entities.
    /// Deletes the donor if count goes to 0.
    /// </summary>
    /// <returns> True if donor moved any count to contacts. </returns>
    [PublicAPI]
    public bool TryMergeToContacts(Entity<StackComponent?, TransformComponent?> donor)
    {
        var (uid, stack, xform) = donor; // sue me
        if (!Resolve(uid, ref stack, ref xform, false))
            return false;

        var map = xform.MapID;
        var bounds = _physics.GetWorldAABB(uid);
        var intersecting = new HashSet<Entity<StackComponent>>(); // Should we reuse a HashSet instead of making a new one?
        _entityLookup.GetEntitiesIntersecting(map, bounds, intersecting, LookupFlags.Dynamic | LookupFlags.Sundries);

        var merged = false;
        foreach (var recipientStack in intersecting)
        {
            var otherEnt = recipientStack.Owner;
            // if you merge a ton of stacks together, you will end up deleting a few by accident.
            if (TerminatingOrDeleted(otherEnt) || EntityManager.IsQueuedForDeletion(otherEnt))
                continue;

            if (!TryMergeStacks((uid, stack), recipientStack.AsNullable(), out _))
                continue;
            merged = true;

            if (stack.Count <= 0)
                break;
        }
        return merged;
    }

    #endregion
    #region Setters

    /// <summary>
    /// Sets a stack count to an amount. Server will delete ent if count is 0.
    /// Clamps between zero and the stack's max size.
    /// </summary>
    /// <remarks> All setter functions should end up here. </remarks>
    public void SetCount(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        // Do nothing if amount is already the same.
        if (amount == ent.Comp.Count)
            return;

        // Store old value for event-raising purposes...
        var old = ent.Comp.Count;

        // Clamp the value.
        amount = Math.Min(amount, GetMaxCount(ent.Comp));
        amount = Math.Max(amount, 0);

        ent.Comp.Count = amount;
        ent.Comp.UiUpdateNeeded = true;
        Dirty(ent);

        Appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count);
        RaiseLocalEvent(ent.Owner, new StackCountChangedEvent(old, ent.Comp.Count));

        // Queue delete stack if count reaches zero.
        if (ent.Comp.Count <= 0)
            PredictedQueueDel(ent.Owner);
    }

    /// <inheritdoc cref="SetCount(Entity{StackComponent?}, int)"/>
    [Obsolete("Use Entity<T> method instead")]
    public void SetCount(EntityUid uid, int amount, StackComponent? component = null)
    {
        SetCount((uid, component), amount);
    }

    // TODO
    /// <summary>
    /// Increase a stack count by an amount, and spawn new entities if above the max.
    /// </summary>
    // public List<EntityUid> RaiseCountAndSpawn(Entity<StackComponent?> ent, int amount);

    /// <summary>
    /// Reduce a stack count by an amount, even if it would go below 0.
    /// If it reaches 0 the stack will despawn.
    /// </summary>
    /// <seealso cref="TryUse"/>
    [PublicAPI]
    public void ReduceCount(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        // Don't reduce unlimited stacks
        if (ent.Comp.Unlimited)
            return;

        SetCount(ent, ent.Comp.Count - amount);
    }

    /// <summary>
    /// Try to reduce a stack count by a whole amount.
    /// Won't reduce the stack count if the amount is larger than the stack.
    /// </summary>
    /// <returns> True if the count was lowered. Always true if the stack is unlimited. </returns>
    [PublicAPI]
    public bool TryUse(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // We're unlimited and always greater than amount
        if (ent.Comp.Unlimited)
            return true;

        // Check if we have enough things in the stack for this...
        if (amount > ent.Comp.Count)
            return false;

        // We do have enough things in the stack, so remove them and change.
        SetCount(ent, ent.Comp.Count - amount);
        return true;
    }

    #endregion
    #region Getters

    /// <summary>
    /// Gets the count in a stack. If it cannot be stacked, returns 1.
    /// </summary>
    [PublicAPI]
    public int GetCount(Entity<StackComponent?> ent)
    {
        return Resolve(ent.Owner, ref ent.Comp, false) ? ent.Comp.Count : 1;
    }

    /// <summary>
    /// Gets the maximum amount that can be fit on a stack.
    /// </summary>
    /// <remarks>
    /// <p>
    /// if there's no StackComponent, this equals 1. Otherwise, if there's a max
    /// count override, it equals that. It then checks for a max count value
    /// on the stack prototype. If there isn't one, it defaults to the max integer
    /// value (unlimited).
    /// </p>
    /// </remarks>
    [PublicAPI]
    public int GetMaxCount(StackComponent? component)
    {
        if (component == null)
            return 1;

        if (component.MaxCountOverride != null)
            return component.MaxCountOverride.Value;

        var stackProto = _prototype.Index(component.StackTypeId);
        return stackProto.MaxCount ?? int.MaxValue;
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntProtoId entityId)
    {
        var entProto = _prototype.Index<EntityPrototype>(entityId);
        entProto.TryGetComponent<StackComponent>(out var stackComp, EntityManager.ComponentFactory);
        return GetMaxCount(stackComp);
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntityPrototype entityId)
    {
        entityId.TryGetComponent<StackComponent>(out var stackComp, EntityManager.ComponentFactory);
        return GetMaxCount(stackComp);
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntityUid uid)
    {
        return GetMaxCount(CompOrNull<StackComponent>(uid));
    }

    /// <summary>
    /// Gets the maximum amount that can be fit on a stack, or int.MaxValue if no max value exists.
    /// </summary>
    [PublicAPI]
    public static int GetMaxCount(StackPrototype stack)
    {
        return stack.MaxCount ?? int.MaxValue;
    }

    /// <inheritdoc cref="GetMaxCount(StackPrototype)"/>
    [PublicAPI]
    public int GetMaxCount(ProtoId<StackPrototype> stackId)
    {
        return GetMaxCount(_prototype.Index(stackId));
    }

    /// <summary>
    /// Gets the remaining space in a stack.
    /// </summary>
    [PublicAPI]
    public int GetAvailableSpace(StackComponent component)
    {
        return GetMaxCount(component) - component.Count;
    }

    #endregion
}
