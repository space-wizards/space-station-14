using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stacks;

/// <summary>
///     Entity system that handles everything relating to stacks.
///     This is a good example for learning how to code in an ECS manner.
/// </summary>
[UsedImplicitly]
public abstract class SharedStackSystem : EntitySystem
{
    [Dependency] private readonly   IGameTiming _gameTiming = default!;
    [Dependency] private readonly   IPrototypeManager _prototype = default!;
    [Dependency] private readonly   IViewVariablesManager _vvm = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] private readonly   EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly   SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly   SharedStorageSystem _storage = default!;

    public static readonly int[] DefaultSplitAmounts = { 1, 5, 10, 20, 30, 50 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, ComponentGetState>(OnStackGetState);
        SubscribeLocalEvent<StackComponent, ComponentHandleState>(OnStackHandleState);
        SubscribeLocalEvent<StackComponent, ComponentStartup>(OnStackStarted);
        SubscribeLocalEvent<StackComponent, ExaminedEvent>(OnStackExamined);
        SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);
        SubscribeLocalEvent<StackComponent, GetVerbsEvent<AlternativeVerb>>(OnStackAlternativeInteract);

        _vvm.GetTypeHandler<StackComponent>()
            .AddPath(nameof(StackComponent.Count), (_, comp) => comp.Count, SetCount);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _vvm.GetTypeHandler<StackComponent>()
            .RemovePath(nameof(StackComponent.Count));
    }

    #region Public API

    /// <summary>
    /// Moves as many stacks as we can from the donor to the recipient.
    /// Deletes the donor if it ran out of stacks.
    /// </summary>
    /// <param name="transferred">How many stacks moved.</param>
    /// <param name="amount">Number of stacks to move from the donor.</param>
    /// <returns>True if transferred is greater than 0.</returns>
    [PublicAPI]
    public bool TryMergeStacks(Entity<StackComponent?> donorEnt,
                                Entity<StackComponent?> recipientEnt,
                                out int transferred,
                                int? amount = null)
    {
        var (donor, donorStack) = donorEnt;
        var (recipient, recipientStack) = recipientEnt;
        transferred = 0;

        if (donor == recipient)
            return false;

        if (!Resolve(recipient, ref recipientStack, false) || !Resolve(donor, ref donorStack, false))
            return false;

        if (recipientStack.StackTypeId != donorStack.StackTypeId)
            return false;

        // The most we can transfer
        transferred = Math.Min(donorStack.Count, GetAvailableSpace(recipientStack));
        if (transferred <= 0)
            return false;

        // transfer only as much as we want
        if (amount != null && amount > 0)
            transferred = Math.Min(transferred, (int)amount);

        SetCount(donorEnt, donorStack.Count - transferred);
        SetCount(recipientEnt, recipientStack.Count + transferred);
        return true;
    }

    /// <summary>
    ///     If the given item is a stack, this attempts to find a matching stack in the users hand, and merge with that.
    /// </summary>
    /// <remarks>
    ///     If the interaction fails to fully merge the stack, or if this is just not a stack, it will instead try
    ///     to place it in the user's hand normally.
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

        // This is shit code until hands get fixed and give an easy way to enumerate over items, starting with the currently active item.
        foreach (var held in Hands.EnumerateHeld(user.Owner, user.Comp))
        {
            TryMergeStacks(item, held, out _);

            if (item.Comp.Count == 0)
                return;
        }

        Hands.PickupOrDrop(user.Owner, item.Owner, handsComp: user.Comp);
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public void TryMergeToHands(EntityUid item, EntityUid user, StackComponent? itemStack = null, HandsComponent? hands = null)
    {
        TryMergeToHands((item, itemStack), (user, hands));
    }

    /// <summary>
    ///     Sets a stack to an amount. Server will delete ent if stack count is 0.
    /// </summary>
    /// <remarks>
    ///     Will not set the amount higher than the stack's max size.
    /// </remarks>
    public virtual void SetCount(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var (stackEnt, stackComp) = (ent.Owner, ent.Comp);

        // Do nothing if amount is already the same.
        if (amount == stackComp.Count)
            return;

        // Store old value for event-raising purposes...
        var old = stackComp.Count;

        // Clamp the value.
        amount = Math.Min(amount, GetMaxCount(stackComp));
        amount = Math.Max(amount, 0);

        // Server-side override deletes the entity if count == 0
        stackComp.Count = amount;
        Dirty(ent);

        Appearance.SetData(stackEnt, StackVisuals.Actual, stackComp.Count);
        RaiseLocalEvent(stackEnt, new StackCountChangedEvent(old, stackComp.Count));

        // Queue delete stack if count reaches zero.
        if (ent.Comp.Count <= 0 && !ent.Comp.Lingering)
            PredictedQueueDel(ent.Owner);
    }

    // TODO
    [Obsolete("Obsolete, Use Entity<T>")]
    public virtual void SetCount(EntityUid uid, int amount, StackComponent? component = null)
    {
        SetCount((uid, component), amount);
    }

    /// <summary>
    ///     Try to use an amount of items on this stack.
    /// </summary>
    /// <returns>True if the stacks were used.</returns>
    [PublicAPI]
    public bool Use(Entity<StackComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // Check if we have enough things in the stack for this...
        if (ent.Comp.Count < amount)
        {
            // Not enough things in the stack, return false.
            return false;
        }

        // We do have enough things in the stack, so remove them and change.
        if (!ent.Comp.Unlimited)
        {
            SetCount(ent, ent.Comp.Count - amount);
        }

        return true;
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public bool Use(EntityUid uid, int amount, StackComponent? stack = null)
    {
        return Use((uid, stack), amount);
    }

    /// <summary>
    /// Donor entity merges stacks with contacting entities.
    /// Deletes donor if all stacks are used.
    /// </summary>
    /// <returns>True if donor moved stacks to contacts.</returns>
    [PublicAPI]
    public bool TryMergeToContacts(Entity<StackComponent?, TransformComponent?> donor)
    {
        if (!Resolve(donor, ref donor.Comp1, ref donor.Comp2, false))
            return false;

        var (uid, stack, xform) = (donor.Owner, donor.Comp1, donor.Comp2); // sue me
        var map = xform.MapID;
        var bounds = _physics.GetWorldAABB(uid);
        var intersecting = new HashSet<Entity<StackComponent>>();
        _entityLookup.GetEntitiesIntersecting(map, bounds, intersecting, LookupFlags.Dynamic | LookupFlags.Sundries);

        var merged = false;
        foreach (var recipientStack in intersecting)
        {
            var otherEnt = recipientStack.Owner;
            // if you merge a ton of stacks together, you will end up deleting a few by accident.
            if (TerminatingOrDeleted(otherEnt) || EntityManager.IsQueuedForDeletion(otherEnt))
                continue;

            if (!TryMergeStacks((uid, stack), (otherEnt, recipientStack), out _))
                continue;
            merged = true;

            if (stack.Count <= 0)
                break;
        }
        return merged;
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public bool TryMergeToContacts(EntityUid uid, StackComponent? stack = null, TransformComponent? xform = null)
    {
        return TryMergeToContacts((uid, stack, xform));
    }

    /// <summary>
    /// Gets the amount of items in a stack. If it cannot be stacked, returns 1.
    /// </summary>
    [PublicAPI]
    public int GetCount(Entity<StackComponent?> ent)
    {
        return Resolve(ent.Owner, ref ent.Comp, false) ? ent.Comp.Count : 1;
    }

    [Obsolete("Obsolete, Use Entity<T>")]
    public int GetCount(EntityUid uid, StackComponent? component = null)
    {
        return GetCount((uid, component));
    }

    /// <summary>
    /// Gets the max stack count for a given entity prototype.
    /// Returns 1 if there's no <see cref="StackComponent"/>.
    /// </summary>
    [PublicAPI]
    public int GetMaxCount(EntProtoId entityId)
    {
        var entProto = _prototype.Index<EntityPrototype>(entityId);
        entProto.TryGetComponent<StackComponent>(out var stackComp, EntityManager.ComponentFactory);
        return GetMaxCount(stackComp);
    }

    /// <summary>
    /// Gets the max stack count for a given entity.
    /// Returns 1 if there's no <see cref="StackComponent"/>.
    /// </summary>
    [PublicAPI]
    public int GetMaxCount(EntityUid uid)
    {
        return GetMaxCount(CompOrNull<StackComponent>(uid));
    }

    /// <summary>
    /// Gets the maximum amount that can be fit on a stack.
    /// </summary>
    /// <remarks>
    /// <p>
    /// if there's no stackcomp, this equals 1. Otherwise, if there's a max
    /// count override, it equals that. It then checks for a max count value
    /// on the prototype. If there isn't one, it defaults to the max integer
    /// value (unlimimted).
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

    /// <summary>
    /// Gets the remaining space in a stack.
    /// </summary>
    [PublicAPI]
    public int GetAvailableSpace(StackComponent component)
    {
        return GetMaxCount(component) - component.Count;
    }

    /// <summary>
    /// Tries to add one stack to another. May have some leftover count in the inserted entity.
    /// </summary>
    [Obsolete("Obsolete, use TryMergeStacks()")]
    public bool TryAdd(EntityUid insertEnt, EntityUid targetEnt, StackComponent? insertStack = null, StackComponent? targetStack = null)
    {
        return TryMergeStacks((insertEnt, insertStack), (targetEnt, targetStack), out var _);
    }

    /// <summary>
    /// Tries to add one stack to another. May have some leftover count in the inserted entity.
    /// </summary>
    [Obsolete("Obsolete, use TryMergeStacks()")]
    public bool TryAdd(EntityUid insertEnt, EntityUid targetEnt, int count, StackComponent? insertStack = null, StackComponent? targetStack = null)
    {
        return TryMergeStacks((insertEnt, insertStack), (targetEnt, targetStack), out var _, count);
    }

    #endregion
    #region Spawning

    /// <summary>
    ///     Try to split this stack into two. Returns a non-null <see cref="Robust.Shared.GameObjects.EntityUid"/> if successful.
    /// </summary>
    public EntityUid? Split(EntityUid uid, int amount, EntityCoordinates spawnPosition, StackComponent? stack = null)
    {
        if (!Resolve(uid, ref stack))
            return null;

        // Try to remove the amount of things we want to split from the original stack...
        if (!Use((uid, stack), amount))
            return null;

        // Get a prototype ID to spawn the new entity. Null is also valid, although it should rarely be picked...
        var prototype = _prototype.TryIndex<StackPrototype>(stack.StackTypeId, out var stackType)
            ? stackType.Spawn.ToString()
            : Prototype(uid)?.ID;

        // Set the output parameter in the event instance to the newly split stack.
        var entity = PredictedSpawnAtPosition(prototype, spawnPosition);

        if (TryComp(entity, out StackComponent? stackComp))
        {
            // Set the split stack's count.
            SetCount((entity, stackComp), amount);
            // Don't let people dupe unlimited stacks
            stackComp.Unlimited = false;
        }

        var ev = new StackSplitEvent(entity);
        RaiseLocalEvent(uid, ref ev);

        return entity;
    }

    /// <summary>
    ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
    /// </summary>
    public EntityUid Spawn(int amount, ProtoId<StackPrototype> id, EntityCoordinates spawnPosition)
    {
        var proto = _prototype.Index(id);
        return Spawn(amount, proto, spawnPosition);
    }

    /// <summary>
    ///     Spawns a stack of a certain stack type. See <see cref="StackPrototype"/>.
    /// </summary>
    public EntityUid Spawn(int amount, StackPrototype prototype, EntityCoordinates spawnPosition)
    {
        // Set the output result parameter to the new stack entity...
        var entity = PredictedSpawnAtPosition(prototype.Spawn, spawnPosition);
        var stack = Comp<StackComponent>(entity);

        // And finally, set the correct amount!
        SetCount((entity, stack), amount);
        return entity;
    }

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
            var entity = PredictedSpawnAtPosition(entityPrototype, spawnPosition);
            spawnedEnts.Add(entity);
            SetCount((entity, null), count);
        }

        return spawnedEnts;
    }

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
            var entity = PredictedSpawnNextToOrDrop(entityPrototype, target);
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
        var proto = _prototype.Index<EntityPrototype>(entityPrototype);
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

    #endregion
    #region Event Handlers

    private void OnStackStarted(Entity<StackComponent> ent, ref ComponentStartup args)
    {
        // on client, lingering stacks that start at 0 need to be darkened
        // on server this does nothing
        SetCount(ent.AsNullable(), ent.Comp.Count);

        if (!TryComp(ent.Owner, out AppearanceComponent? appearance))
            return;

        Appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Count, appearance);
        Appearance.SetData(ent.Owner, StackVisuals.MaxCount, GetMaxCount(ent.Comp), appearance);
        Appearance.SetData(ent.Owner, StackVisuals.Hide, false, appearance);
    }

    private void OnStackGetState(Entity<StackComponent> ent, ref ComponentGetState args)
    {
        args.State = new StackComponentState(ent.Comp.Count, ent.Comp.MaxCountOverride, ent.Comp.Lingering);
    }

    private void OnStackHandleState(Entity<StackComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not StackComponentState cast)
            return;

        ent.Comp.MaxCountOverride = cast.MaxCount;
        ent.Comp.Lingering = cast.Lingering;
        // This will change the count and call events.
        SetCount(ent.AsNullable(), cast.Count);
    }

    private void OnStackExamined(Entity<StackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString("comp-stack-examine-detail-count",
                ("count", ent.Comp.Count),
                ("markupCountColor", "lightgray")
            )
        );
    }

    private void OnStackInteractUsing(Entity<StackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<StackComponent>(args.Used, out var recipientStack))
            return;

        // Transfer stacks from ground to hand
        if (!TryMergeStacks((ent.Owner, ent.Comp), (args.Used, recipientStack), out var transferred))
            return; // if nothing transfered, leave without a pop-up

        args.Handled = true;

        // interaction is done, the rest is just generating a pop-up

        var popupPos = args.ClickLocation;
        var userCoords = Transform(args.User).Coordinates;

        if (!popupPos.IsValid(EntityManager))
        {
            popupPos = userCoords;
        }

        switch (transferred)
        {
            case > 0:
                Popup.PopupClient($"+{transferred}", popupPos, args.User);

                if (GetAvailableSpace(recipientStack) == 0)
                {
                    Popup.PopupClient(Loc.GetString("comp-stack-becomes-full"),
                        popupPos.Offset(new Vector2(0, -0.5f)), args.User);
                }

                break;

            case 0 when GetAvailableSpace(recipientStack) == 0:
                Popup.PopupClient(Loc.GetString("comp-stack-already-full"), popupPos, args.User);
                break;
        }

        var localRotation = Transform(args.Used).LocalRotation;
        _storage.PlayPickupAnimation(args.Used, popupPos, userCoords, localRotation, args.User);
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
            Popup.PopupPredictedCursor(Loc.GetString("comp-stack-split-too-small"), user.Owner, PopupType.Medium);
            return;
        }

        if (Split(stack.Owner, amount, user.Comp.Coordinates, stack) is not {} split)
            return;

        Hands.PickupOrDrop(user.Owner, split);

        Popup.PopupPredictedCursor(Loc.GetString("comp-stack-split"), user.Owner);
    }
    #endregion
}

/// <summary>
///     Event raised when a stack's count has changed.
/// </summary>
public sealed class StackCountChangedEvent : EntityEventArgs
{
    /// <summary>
    ///     The old stack count.
    /// </summary>
    public int OldCount;

    /// <summary>
    ///     The new stack count.
    /// </summary>
    public int NewCount;

    public StackCountChangedEvent(int oldCount, int newCount)
    {
        OldCount = oldCount;
        NewCount = newCount;
    }
}
