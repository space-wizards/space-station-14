using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stacks;

// Partial for public API functions.
public abstract partial class SharedStackSystem
{
    #region Spawning
    // Interactions with spawned entities can not currently be predicted.
    // This means that when spawning a stack it should not be given directly to the player, but have some intermediary.

    /// <summary>
    /// Gets or spawns an entity with a stack count of 1.
    /// Useful when you don't know if something is a stack, and want to make sure you just have a single entity.
    /// </summary>
    /// <param name="stackEnt">An entity to pop one count off the stack.</param>
    /// <returns>An entity with a stack count of 1, or a non-stack.</returns>
    [PublicAPI]
    public EntityUid GetOne(Entity<StackComponent?> stackEnt)
    {
        if (!Resolve(stackEnt.Owner, ref stackEnt.Comp, logMissing: false) // If it's not a stack, you already have the one
            || stackEnt.Comp.Count == 1) // If it's at one, just use this
            return stackEnt.Owner;

        ReduceCount(stackEnt, 1);
        var stackId = ProtoMan.Index(stackEnt.Comp.StackTypeId);
        var entityUid = PredictedSpawnNextToOrDrop(stackId.Spawn, stackEnt.Owner);

        SetCount(entityUid, 1);
        return entityUid;
    }

    #endregion
    #region Merge Stacks

    /// <summary>
    /// Merges together the counts from a set of stacks into the smallest number of entities.
    /// </summary>
    /// <param name="stacks">Entities to merge. Returns stack entities with non-zero count.</param>
    [PublicAPI]
    public void MergeStacks(ref HashSet<EntityUid> stacks)
    {
        // Filter out the non-stacks and separate them by their stack types
        var stacksByType = new Dictionary<ProtoId<StackPrototype>, List<Entity<StackComponent>>>();
        foreach (var uid in stacks)
        {
            if (!_stackQuery.TryComp(uid, out var stackComponent))
                continue;

            if (stacksByType.TryGetValue(stackComponent.StackTypeId, out var list))
                list.Add((uid, stackComponent));
            else
            {
                list = new List<Entity<StackComponent>>();
                list.Add((uid, stackComponent));
                stacksByType[stackComponent.StackTypeId] = list;
            }
        }

        stacks.Clear();

        // Set the count
        foreach (var (type, stackList) in stacksByType)
        {
            var count = GetCount(stackList, type);

            foreach (var stack in stackList)
            {
                // We've already moved all our stacks, so we clear the count of the remaining ones.
                if (count == 0)
                {
                    SetCount(stack.AsNullable(), count);
                    continue;
                }

                var amount = Math.Min(count, GetMaxCount(stack.Comp));
                SetCount(stack.AsNullable(), amount);

                count -= amount;
                stacks.Add(stack);
            }
        }
    }

    /// <summary>
    /// This will find all the stacks in an area and merge them together.
    /// </summary>
    /// <remarks>
    /// Useful for when you're spawning an unknown number of stacks like from an entity table
    /// and want to combine them together at the end.
    /// </remarks>
    /// <returns>Stack entities with non-zero counts.</returns>
    [PublicAPI]
    public HashSet<EntityUid> MergeStacksAtPosition(MapCoordinates pos, float range = 0.5f, LookupFlags flags = EntityLookupSystem.DefaultFlags)
    {
        var entities = _entityLookup.GetEntitiesInRange(pos, range, flags);
        MergeStacks(ref entities);
        return entities;
    }

    /// <summary>
    /// Moves as much stack count as we can from the donor to the recipient.
    /// Deletes the donor if count goes to 0.
    /// </summary>
    /// <param name="donor">Entity losing count.</param>
    /// <param name="recipient">Entity gaining count.</param>
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

        // Check they're stacks of the same type
        if (!_stackQuery.Resolve(recipient, ref recipient.Comp, false)
            || !_stackQuery.Resolve(donor, ref donor.Comp, false)
            || recipient.Comp.StackTypeId != donor.Comp.StackTypeId)
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

        if (!_stackQuery.Resolve(item.Owner, ref item.Comp, false))
        {
            // This isn't even a stack. Just try to pickup as normal.
            _hands.PickupOrDrop(user.Owner, item.Owner, handsComp: user.Comp);
            return;
        }

        foreach (var held in _hands.EnumerateHeld(user))
        {
            TryMergeStacks(item, held, out _);

            if (item.Comp.Count == 0)
                return;
        }

        _hands.PickupOrDrop(user.Owner, item.Owner, handsComp: user.Comp);
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

        return TryMergeToStacks((uid, stack), intersecting);
    }

    /// <summary>
    /// Moves the count from the donor into the collection of entities.
    /// </summary>
    /// <returns>True if anything moved.</returns>
    [PublicAPI]
    public bool TryMergeToStacks(Entity<StackComponent?> donor, HashSet<Entity<StackComponent>> stacks)
    {
        if (!_stackQuery.Resolve(donor.Owner, ref donor.Comp, false))
            return false;

        var count = GetCount(donor);
        foreach (var stack in stacks)
        {
            if (stack.Comp.StackTypeId != donor.Comp.StackTypeId)
                continue;

            TryMergeStacks(donor, stack.AsNullable(), out var transferred);

            count -= transferred;
            if (count == 0)
                break;
        }

        return true;
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
        if (!_stackQuery.Resolve(ent.Owner, ref ent.Comp))
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

        _appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count);
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
        if (!_stackQuery.Resolve(ent.Owner, ref ent.Comp))
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
        if (!_stackQuery.Resolve(ent.Owner, ref ent.Comp))
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
        return _stackQuery.Resolve(ent.Owner, ref ent.Comp, false) ? ent.Comp.Count : 1;
    }

    /// <summary>
    /// Gets the total count from a list of stacks.
    /// </summary>
    private int GetCount(List<Entity<StackComponent>> stacks, ProtoId<StackPrototype> id)
    {
        var count = 0;
        foreach (var (_, stack) in stacks)
        {
            if (stack.StackTypeId != id)
                continue;

            count += stack.Count;
        }

        return count;
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

        var stackProto = ProtoMan.Index(component.StackTypeId);
        return stackProto.MaxCount ?? int.MaxValue;
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntProtoId entityId)
    {
        var entProto = ProtoMan.Index<EntityPrototype>(entityId);
        entProto.TryComp<StackComponent>(out var stackComp, EntityManager.ComponentFactory);
        return GetMaxCount(stackComp);
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntityPrototype entityId)
    {
        if (!entityId.TryComp<StackComponent>(out var stackComp, EntityManager.ComponentFactory))
            return 1;

        return GetMaxCount(stackComp);
    }

    /// <inheritdoc cref="GetMaxCount(StackComponent?)"/>
    [PublicAPI]
    public int GetMaxCount(EntityUid uid)
    {
        return GetMaxCount(_stackQuery.CompOrNull(uid));
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
        return GetMaxCount(ProtoMan.Index(stackId));
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
