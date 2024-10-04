using System.Linq;
using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stacks;

[UsedImplicitly]
public abstract class SharedStackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    private const float AreaInsertDelayPerItem = 0.025f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, ComponentGetState>(OnStackGetState);
        SubscribeLocalEvent<StackComponent, ComponentHandleState>(OnStackHandleState);
        SubscribeLocalEvent<StackComponent, ComponentStartup>(OnStackStarted);
        SubscribeLocalEvent<StackComponent, ExaminedEvent>(OnStackExamined);
        SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);
        SubscribeLocalEvent<StackComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StackComponent, StackAreaInsertEvent>(OnDoAfter);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _vvm.GetTypeHandler<StackComponent>()
            .RemovePath(nameof(StackComponent.Count));
    }

    private void OnStackInteractUsing(Entity<StackComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out StackComponent? recipientStack))
            return;

        var localRotation = Transform(args.Used).LocalRotation;

        if (!TryComp<StackComponent>(args.Used, out var usedStackComp))
            return;

        if (!TryMergeStacks(entity, (args.Used, usedStackComp), out var transferred))
            return;

        args.Handled = true;

        // interaction is done, the rest is just generating a pop-up

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var popupPos = args.ClickLocation;
        var userCoords = Transform(args.User).Coordinates;

        if (!popupPos.IsValid(EntityManager))
        {
            popupPos = userCoords;
        }

        switch (transferred)
        {
            case > 0:
                Popup.PopupCoordinates($"+{transferred}", popupPos, Filter.Local(), false);

                if (GetAvailableSpace(recipientStack) == 0)
                {
                    Popup.PopupCoordinates(Loc.GetString("comp-stack-becomes-full"),
                        popupPos.Offset(new Vector2(0, -0.5f)), Filter.Local(), false);
                }

                break;

            case 0 when GetAvailableSpace(recipientStack) == 0:
                Popup.PopupCoordinates(Loc.GetString("comp-stack-already-full"), popupPos, Filter.Local(), false);
                break;
        }

        _storage.PlayPickupAnimation(args.Used, popupPos, userCoords, localRotation, args.User);
    }

    /// <summary>
    /// Allows the user to pick up entities in an area that combine with the stack
    /// </summary>
    private void OnAfterInteract(Entity<StackComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!_prototype.TryIndex<StackPrototype>(entity.Comp.StackTypeId, out var stackPrototype))
            return;

        // The itemSize weight is used in calculations for the doafter delay
        if (!TryComp<ItemComponent>(entity, out var itemComp) || !_prototype.TryIndex(itemComp.Size, out var itemSize))
            return;

        var stackEntities = _entityLookupSystem.GetEntitiesInRange<StackComponent>(args.ClickLocation,
            entity.Comp.AreaInsertRadius,
            LookupFlags.Dynamic | LookupFlags.Sundries);

        // We'll only look for enough stacks to fill up the stack in our hand
        // The delay will be based on the total amount we add to the stack
        var amountAdded = 0;
        int? amountTillMax = null;
        if (stackPrototype.MaxCount != null)
        {
            if (stackPrototype.MaxCount == entity.Comp.Count)
                return;

            amountTillMax = stackPrototype.MaxCount - entity.Comp.Count;
        }

        // create a list of stacks to merge into the used item
        // until we get enough to reach maximum capacity
        List<EntityUid> stacksToMerge = [];
        foreach (var stack in stackEntities)
        {
            // prevent collection of items that are unreachable
            if (stack.Owner == args.User || !_interactionSystem.InRangeUnobstructed(args.User, stack.Owner))
                continue;

            // only merge stacks that are the same kind of stack
            if (string.IsNullOrEmpty(entity.Comp.StackTypeId) || !entity.Comp.StackTypeId.Equals(stack.Comp.StackTypeId))
                continue;

            stacksToMerge.Add(stack);
            if (amountTillMax != null)
            {
                // stop looking for stacks after we get enough to reach the max value stack
                // if there even is a max amount for the stack (there are some unlimited stacks)
                amountAdded += Math.Clamp(stack.Comp.Count, 0, amountTillMax.Value);

                if (amountTillMax == amountAdded)
                    break;
            }
            else
            {
                amountAdded = stack.Comp.Count;
            }
        }

        // return if nothing to merge into the stack in hand
        if (stacksToMerge.Count <= 0)
            return;

        // delay is calculated similar to how storage comp area insert works,
        // but we use the stack count rather than different entity count
        var delay = 0f;
        delay += itemSize.Weight * amountAdded * AreaInsertDelayPerItem;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new StackAreaInsertEvent(GetNetEntityList(stacksToMerge)), entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    /// <summary>
    /// Area insert do-after that allows you to merge stacked items from the floor into your hand
    /// </summary>
    private void OnDoAfter(Entity<StackComponent> entity, ref StackAreaInsertEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        foreach (var stack in args.StacksToMerge)
        {
            var stackEnt = GetEntity(stack);
            if (TryComp<StackComponent>(stackEnt, out var stackComponent))
                TryMergeStacks((stackEnt, stackComponent), entity, out _);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Attempts to merge stacks together if their stackTypeId matches each other
    /// </summary>
    private bool TryMergeStacks(Entity<StackComponent> donor, Entity<StackComponent> recipient, out int transferred)
    {
        transferred = 0;

        if (donor == recipient)
            return false;

        if (string.IsNullOrEmpty(recipient.Comp.StackTypeId) || !recipient.Comp.StackTypeId.Equals(donor.Comp.StackTypeId))
            return false;

        transferred = Math.Min(donor.Comp.Count, GetAvailableSpace(recipient.Comp));
        SetCount(donor, donor.Comp.Count - transferred);
        SetCount(recipient, recipient.Comp.Count + transferred);
        return transferred > 0;
    }

    /// <summary>
    ///     If the given item is a stack, this attempts to find a matching stack in the users hand, and merge with that.
    /// </summary>
    /// <remarks>
    ///     If the interaction fails to fully merge the stack, or if this is just not a stack, it will instead try
    ///     to place it in the user's hand normally.
    /// </remarks>
    public void TryMergeToHands(
        EntityUid item,
        EntityUid user,
        StackComponent? itemStack = null,
        HandsComponent? hands = null)
    {
        if (!Resolve(user, ref hands, false))
            return;

        if (!Resolve(item, ref itemStack, false))
        {
            // This isn't even a stack. Just try to pickup as normal.
            Hands.PickupOrDrop(user, item, handsComp: hands);
            return;
        }

        // This is shit code until hands get fixed and give an easy way to enumerate over items, starting with the currently active item.
        foreach (var held in Hands.EnumerateHeld(user, hands))
        {
            if (TryComp<StackComponent>(held, out var heldStackComp))
                TryMergeStacks((item, itemStack), (held, heldStackComp), out _);

            if (itemStack.Count == 0)
                return;
        }

        Hands.PickupOrDrop(user, item, handsComp: hands);
    }

    public virtual void SetCount(Entity<StackComponent> entity, int amount)
    {
        // Do nothing if amount is already the same.
        if (amount == entity.Comp.Count)
            return;

        // Store old value for event-raising purposes...
        var old = entity.Comp.Count;

        // Clamp the value.
        amount = Math.Min(amount, GetMaxCount(entity.Comp));
        amount = Math.Max(amount, 0);

        // Server-side override deletes the entity if count == 0
        entity.Comp.Count = amount;
        Dirty(entity, entity.Comp);

        _appearance.SetData(entity, StackVisuals.Actual, entity.Comp.Count);
        RaiseLocalEvent(entity, new StackCountChangedEvent(old, entity.Comp.Count));
    }

    /// <summary>
    ///     Try to use an amount of items on this stack. Returns whether this succeeded.
    /// </summary>
    public bool Use(Entity<StackComponent> entity, int amount)
    {
        // Check if we have enough things in the stack for this...
        if (entity.Comp.Count < amount)
        {
            // Not enough things in the stack, return false.
            return false;
        }

        // We do have enough things in the stack, so remove them and change.
        if (!entity.Comp.Unlimited)
        {
            SetCount(entity, entity.Comp.Count - amount);
        }

        return true;
    }

    /// <summary>
    /// Tries to merge a stack into any of the stacks it is touching.
    /// </summary>
    /// <returns>Whether or not it was successfully merged into another stack</returns>
    public bool TryMergeToContacts(EntityUid uid, StackComponent? stack = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref stack, ref xform, false))
            return false;

        var map = xform.MapID;
        var bounds = _physics.GetWorldAABB(uid);
        var intersecting = new HashSet<Entity<StackComponent>>();
        _entityLookup.GetEntitiesIntersecting(map, bounds, intersecting, LookupFlags.Dynamic | LookupFlags.Sundries);

        var merged = false;
        foreach (var otherStack in intersecting)
        {
            var otherEnt = otherStack.Owner;
            // if you merge a ton of stacks together, you will end up deleting a few by accident.
            if (TerminatingOrDeleted(otherEnt) || EntityManager.IsQueuedForDeletion(otherEnt))
                continue;

            if (!TryMergeStacks((uid, stack), (otherEnt, otherStack), out _))
                continue;
            merged = true;

            if (stack.Count <= 0)
                break;
        }
        return merged;
    }

    /// <summary>
    /// Gets the amount of items in a stack. If it cannot be stacked, returns 1.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public int GetCount(EntityUid uid, StackComponent? component = null)
    {
        return Resolve(uid, ref component, false) ? component.Count : 1;
    }

    /// <summary>
    /// Gets the max count for a given entity prototype
    /// </summary>
    /// <param name="entityId"></param>
    /// <returns></returns>
    [PublicAPI]
    public int GetMaxCount(string entityId)
    {
        var entProto = _prototype.Index<EntityPrototype>(entityId);
        entProto.TryGetComponent<StackComponent>(out var stackComp);
        return GetMaxCount(stackComp);
    }

    /// <summary>
    /// Gets the max count for a given entity
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
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
    /// <param name="component"></param>
    /// <returns></returns>
    public int GetMaxCount(StackComponent? component)
    {
        if (component == null)
            return 1;

        if (component.MaxCountOverride != null)
            return component.MaxCountOverride.Value;

        if (string.IsNullOrEmpty(component.StackTypeId))
            return 1;

        var stackProto = _prototype.Index<StackPrototype>(component.StackTypeId);

        return stackProto.MaxCount ?? int.MaxValue;
    }

    /// <summary>
    /// Gets the remaining space in a stack.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    [PublicAPI]
    public int GetAvailableSpace(StackComponent component)
    {
        return GetMaxCount(component) - component.Count;
    }

    /// <summary>
    /// Tries to add one stack to another. May have some leftover count in the inserted entity.
    /// </summary>
    public bool TryAdd(EntityUid insertEnt, EntityUid targetEnt, StackComponent? insertStack = null, StackComponent? targetStack = null)
    {
        if (!Resolve(insertEnt, ref insertStack) || !Resolve(targetEnt, ref targetStack))
            return false;

        var count = insertStack.Count;
        return TryAdd(insertEnt, targetEnt, count, insertStack, targetStack);
    }

    /// <summary>
    /// Tries to add one stack to another. May have some leftover count in the inserted entity.
    /// </summary>
    public bool TryAdd(EntityUid insertEnt, EntityUid targetEnt, int count, StackComponent? insertStack = null, StackComponent? targetStack = null)
    {
        if (!Resolve(insertEnt, ref insertStack) || !Resolve(targetEnt, ref targetStack))
            return false;

        if (insertStack.StackTypeId != targetStack.StackTypeId)
            return false;

        var available = GetAvailableSpace(targetStack);

        if (available <= 0)
            return false;

        var change = Math.Min(available, count);

        SetCount((targetEnt, targetStack), targetStack.Count + change);
        SetCount((insertEnt, insertStack), insertStack.Count - change);
        return true;
    }

    private void OnStackStarted(Entity<StackComponent> entity, ref ComponentStartup args)
    {
        // on client, lingering stacks that start at 0 need to be darkened
        // on server this does nothing
        SetCount(entity, entity.Comp.Count);

        if (!TryComp(entity, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(entity, StackVisuals.Actual, entity.Comp.Count, appearance);
        _appearance.SetData(entity, StackVisuals.MaxCount, GetMaxCount(entity.Comp), appearance);
        _appearance.SetData(entity, StackVisuals.Hide, false, appearance);
    }

    private void OnStackGetState(EntityUid uid, StackComponent component, ref ComponentGetState args)
    {
        args.State = new StackComponentState(component.Count, component.MaxCountOverride, component.Lingering);
    }

    private void OnStackHandleState(Entity<StackComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not StackComponentState cast)
            return;

        entity.Comp.MaxCountOverride = cast.MaxCount;
        entity.Comp.Lingering = cast.Lingering;
        // This will change the count and call events.
        SetCount(entity, cast.Count);
    }

    private void OnStackExamined(Entity<StackComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString("comp-stack-examine-detail-count",
                ("count", entity.Comp.Count),
                ("markupCountColor", "lightgray")
            )
        );
    }
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
