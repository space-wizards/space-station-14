using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Stack;

/// <summary>
///     Entity system that handles everything relating to stacks.
///     This is a good example for learning how to code in an ECS manner.
/// </summary>
[UsedImplicitly]
public sealed class StackSystem : SharedStackSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, GetVerbsEvent<AlternativeVerb>>(OnStackAlternativeInteract);
    }

    /// <summary>
    /// Sets the entity's count, and deletes it if it's 0 or less.
    /// </summary>
    public override void SetCount(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        base.SetCount(ent, amount);

        // Queue delete stack if count reaches zero.
        if (ent.Comp.Count <= 0 && !ent.Comp.Lingering)
            QueueDel(ent.Owner);
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public override void SetCount(EntityUid uid, int amount, StackComponent? component = null)
    {
        SetCount((uid, component), amount);
    }

    #region Spawning

    /// <summary>
    ///     Spawns a new entity and moves an amount to it from ent.
    /// </summary>
    /// <param name="amount"> How much to move to the new entity. </param>
    /// <returns>Null if StackComponent doesn't resolve, or amount is greater than ent.Comp.Count.</returns>
    [PublicAPI]
    public EntityUid? Split(Entity<StackComponent?> ent, int amount, EntityCoordinates spawnPosition)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return null;

        // Try to remove the amount of things we want to split from the original stack...
        if (!Use(ent, amount))
            return null;

        // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
        var prototype = _prototypeManager.TryIndex<StackPrototype>(ent.Comp.StackTypeId, out var stackType)
            ? stackType.Spawn.ToString()
            : Prototype(ent.Owner)?.ID;

        // Set the output parameter in the event instance to the newly split stack.
        var newEntity = SpawnAtPosition(prototype, spawnPosition);

        // There should always be a StackComponent, but make sure
        var stackComp = EnsureComp<StackComponent>(newEntity);

        SetCount((newEntity, stackComp), amount);
        stackComp.Unlimited = false; // Don't let people dupe unlimited stacks

        var ev = new StackSplitEvent(newEntity);
        RaiseLocalEvent(ent, ref ev);

        return newEntity;
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public EntityUid? Split(EntityUid uid, int amount, EntityCoordinates spawnPosition, StackComponent? stack = null)
    {
        return Split((uid, stack), amount, spawnPosition);
    }

    #region SpawnAtPosition

    /// <summary>
    ///     Spawns a stack of a certain stack type and sets its count. Won't set the stack over its max.
    /// </summary>
    /// <param name="count"> The amount to set the spawned stack to.</param>
    [PublicAPI]
    public EntityUid SpawnAtPosition(int count, StackPrototype prototype, EntityCoordinates spawnPosition)
    {
        // Set the output result parameter to the new stack entity...
        var entity = SpawnAtPosition(prototype.Spawn, spawnPosition);  // The real SpawnAtPosition
        var stack = Comp<StackComponent>(entity);

        // And finally, set the correct amount!
        SetCount((entity, stack), count);
        return entity;
    }

    /// <inheritdoc cref="SpawnAtPosition"/>
    [PublicAPI]
    public EntityUid SpawnAtPosition(int count, ProtoId<StackPrototype> id, EntityCoordinates spawnPosition)
    {
        var proto = _prototypeManager.Index(id);
        return SpawnAtPosition(count, proto, spawnPosition);
    }

    /// <summary>
    ///     Say you want to spawn 97 units of something that has a max stack count of 30.
    ///     This would spawn 3 stacks of 30 and 1 stack of 7.
    /// </summary>
    /// <returns>The entities spawned.</returns>
    private List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototype, List<int> amounts, EntityCoordinates spawnPosition)
    {
        if (amounts.Count <= 0)
        {
            Log.Error(
                $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in amounts)
        {
            var entity = SpawnAtPosition(entityPrototype, spawnPosition);
            spawnedEnts.Add(entity);
            SetCount((entity, null), count);
        }

        return spawnedEnts;
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(EntityPrototype entityProto, int amount, EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(entityProto.ID, CalculateSpawns(entityProto, amount), spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototypeID, int amount, EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(entityPrototypeID, CalculateSpawns(entityPrototypeID, amount), spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(StackPrototype stack, int amount, EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(stack.Spawn, CalculateSpawns(stack, amount), spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(ProtoId<StackPrototype> stackID, int amount, EntityCoordinates spawnPosition)
    {
        var stackProto = _prototypeManager.Index<StackPrototype>(stackID);
        return SpawnMultipleAtPosition(stackProto.Spawn, CalculateSpawns(stackProto, amount), spawnPosition);
    }

    #endregion
    #region SpawnNextToOrDrop

    /// <inheritdoc cref="SpawnAtPosition"/>
    [PublicAPI]
    public EntityUid SpawnNextToOrDrop(int amount, ProtoId<StackPrototype> id, EntityUid source)
    {
        var proto = _prototypeManager.Index(id);
        return SpawnNextToOrDrop(amount, proto, source);
    }

    /// <inheritdoc cref="SpawnAtPosition"/>
    [PublicAPI]
    public EntityUid SpawnNextToOrDrop(int amount, StackPrototype prototype, EntityUid source)
    {
        var entity = SpawnNextToOrDrop(prototype.Spawn, source); // The real SpawnNextToOrDrop
        var stack = Comp<StackComponent>(entity);

        SetCount((entity, stack), amount);
        return entity;
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition"/>
    private List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId entityPrototype, List<int> amounts, EntityUid target)
    {
        if (amounts.Count <= 0)
        {
            Log.Error(
                $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in amounts)
        {
            var entity = SpawnNextToOrDrop(entityPrototype, target);
            spawnedEnts.Add(entity);
            SetCount((entity, null), count);
        }

        return spawnedEnts;
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(EntityPrototype stack, int amount, EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack.ID, CalculateSpawns(stack, amount), target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId stack, int amount, EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack, CalculateSpawns(stack, amount), target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(StackPrototype stack, int amount, EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack.Spawn, CalculateSpawns(stack, amount), target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(ProtoId<StackPrototype> stackID, int amount, EntityUid target)
    {
        var stackProto = _prototypeManager.Index<StackPrototype>(stackID);
        return SpawnMultipleNextToOrDrop(stackProto.Spawn, CalculateSpawns(stackProto, amount), target);
    }

    #endregion
    #region Calculate

    /// <summary>
    ///     Calculates how many stacks to spawn that total up to <paramref name="amount"/>.
    /// </summary>
    /// <returns>The list of stack counts per entity.</returns>
    private List<int> CalculateSpawns(int maxCountPerStack, int amount)
    {
        var amounts = new List<int>();
        while (amount > 0)
        {
            var countAmount = Math.Min(maxCountPerStack, amount);
            amount -= countAmount;
            amounts.Add(countAmount);
        }

        return amounts;
    }

    /// <inheritdoc cref="CalculateSpawns"/>
    private List<int> CalculateSpawns(StackPrototype stackProto, int amount)
    {
        return CalculateSpawns(GetMaxCount(stackProto), amount);
    }

    /// <inheritdoc cref="CalculateSpawns"/>
    private List<int> CalculateSpawns(EntityPrototype entityPrototype, int amount)
    {
        return CalculateSpawns(GetMaxCount(entityPrototype), amount);
    }

    /// <inheritdoc cref="CalculateSpawns"/>
    private List<int> CalculateSpawns(EntProtoId entityId, int amount)
    {
        return CalculateSpawns(GetMaxCount(entityId), amount);
    }

    #endregion
    #endregion
    #region  Event Handlers

    private void OnStackAlternativeInteract(Entity<StackComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.Count == 1)
            return;

        var (uid, stack) = ent;
        var user = args.User; // Can't pass ref events into verbs

        AlternativeVerb halve = new()
        {
            Text = Loc.GetString("comp-stack-split-halve"),
            Category = VerbCategory.Split,
            Act = () => UserSplit(ent, user, stack.Count / 2),
            Priority = 1
        };
        args.Verbs.Add(halve);

        var priority = 0;
        foreach (var amount in DefaultSplitAmounts)
        {
            if (amount >= stack.Count)
                continue;

            AlternativeVerb verb = new()
            {
                Text = amount.ToString(),
                Category = VerbCategory.Split,
                Act = () => UserSplit(ent, user, amount),
                // we want to sort by size, not alphabetically by the verb text.
                Priority = priority
            };

            priority--;

            args.Verbs.Add(verb);
        }
    }

    private void UserSplit(Entity<StackComponent> stack, Entity<TransformComponent?> user, int amount)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return;

        if (amount <= 0)
        {
            Popup.PopupCursor(Loc.GetString("comp-stack-split-too-small"), user.Owner, PopupType.Medium);
            return;
        }

        if (Split(stack.AsNullable(), amount, user.Comp.Coordinates) is not { } split)
            return;

        Hands.PickupOrDrop(user.Owner, split);

        Popup.PopupCursor(Loc.GetString("comp-stack-split"), user.Owner);
    }
    #endregion
}
