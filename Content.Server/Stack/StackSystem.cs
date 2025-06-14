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

    /// <summary>
    ///     Try to split this stack into two. Returns a non-null <see cref="Robust.Shared.GameObjects.EntityUid"/> if successful.
    /// </summary>
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
        var newEntity = Spawn(prototype, spawnPosition);

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

    /// <summary>
    ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
    /// </summary>
    public EntityUid Spawn(int amount, ProtoId<StackPrototype> id, EntityCoordinates spawnPosition)
    {
        var proto = _prototypeManager.Index(id);
        return Spawn(amount, proto, spawnPosition);
    }

    /// <summary>
    ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
    /// </summary>
    public EntityUid Spawn(int amount, StackPrototype prototype, EntityCoordinates spawnPosition)
    {
        // Set the output result parameter to the new stack entity...
        var entity = SpawnAtPosition(prototype.Spawn, spawnPosition);
        var stack = Comp<StackComponent>(entity);

        // And finally, set the correct amount!
        SetCount((entity, stack), amount);
        return entity;
    }

    // /// <summary>
    // ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
    // /// </summary>
    // public EntityUid SpawnNextToOrDrop(int amount, ProtoId<StackPrototype> id, EntityUid source)
    // {
    //     var proto = _prototypeManager.Index(id);
    //     return SpawnNextToOrDrop(amount, proto, source);
    // }

    // ///
    // public EntityUid SpawnNextToOrDrop(int amount, StackPrototype prototype, EntityUid source)
    // {
    //     var entity = SpawnNextToOrDrop(prototype.Spawn, source);
    //     var stack = Comp<StackComponent>(entity);

    //     SetCount((entity, stack), amount);
    //     return entity;
    // }

    /// <summary>
    ///     Say you want to spawn 97 units of something that has a max stack count of 30.
    ///     This would spawn 3 stacks of 30 and 1 stack of 7.
    /// </summary>
    public List<EntityUid> SpawnMultiple(string entityPrototype, int amount, EntityCoordinates spawnPosition)
    {
        if (amount <= 0)
        {
            Log.Error(
                $"Attempted to spawn an invalid stack: {entityPrototype}, {amount}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawns = CalculateSpawns(entityPrototype, amount);

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in spawns)
        {
            var entity = SpawnAtPosition(entityPrototype, spawnPosition);
            spawnedEnts.Add(entity);
            SetCount((entity, null), count);
        }

        return spawnedEnts;
    }

    // TODO
    // List<EntityUid> SpawnNexrToOrDropMultiple(string entityPrototype, int amount, EntityUid source)

    /// <inheritdoc cref="SpawnMultiple(string,int,EntityCoordinates)"/>
    public List<EntityUid> SpawnMultiple(string entityPrototype, int amount, EntityUid target)
    {
        if (amount <= 0)
        {
            Log.Error(
                $"Attempted to spawn an invalid stack: {entityPrototype}, {amount}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawns = CalculateSpawns(entityPrototype, amount);

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in spawns)
        {
            var entity = SpawnNextToOrDrop(entityPrototype, target);
            spawnedEnts.Add(entity);
            SetCount((entity, null), count);
        }

        return spawnedEnts;
    }

    /// <summary>
    /// Calculates how many stacks to spawn that total up to <paramref name="amount"/>.
    /// </summary>
    /// <param name="entityPrototype">The stack to spawn.</param>
    /// <param name="amount">The amount of pieces across all stacks.</param>
    /// <returns>The list of stack counts per entity.</returns>
    private List<int> CalculateSpawns(string entityPrototype, int amount)
    {
        var proto = _prototypeManager.Index<EntityPrototype>(entityPrototype);
        proto.TryGetComponent<StackComponent>(out var stack, EntityManager.ComponentFactory);
        var maxCountPerStack = GetMaxCount(stack);
        var amounts = new List<int>();
        while (amount > 0)
        {
            var countAmount = Math.Min(maxCountPerStack, amount);
            amount -= countAmount;
            amounts.Add(countAmount);
        }

        return amounts;
    }

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

        if (Split(stack.Owner, amount, user.Comp.Coordinates, stack) is not {} split)
            return;

        Hands.PickupOrDrop(user.Owner, split);

        Popup.PopupCursor(Loc.GetString("comp-stack-split"), user.Owner);
    }
}
