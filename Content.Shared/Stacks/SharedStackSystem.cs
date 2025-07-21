using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stacks
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IViewVariablesManager _vvm = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
        [Dependency] protected readonly SharedHandsSystem Hands = default!;
        [Dependency] protected readonly SharedTransformSystem Xform = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] protected readonly SharedPopupSystem Popup = default!;
        [Dependency] private readonly SharedStorageSystem _storage = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StackComponent, ComponentGetState>(OnStackGetState);
            SubscribeLocalEvent<StackComponent, ComponentHandleState>(OnStackHandleState);
            SubscribeLocalEvent<StackComponent, ComponentStartup>(OnStackStarted);
            SubscribeLocalEvent<StackComponent, ExaminedEvent>(OnStackExamined);
            SubscribeLocalEvent<StackComponent, InteractUsingEvent>(OnStackInteractUsing);

            _vvm.GetTypeHandler<StackComponent>()
                .AddPath(nameof(StackComponent.Count), (_, comp) => comp.Count, SetCount);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _vvm.GetTypeHandler<StackComponent>()
                .RemovePath(nameof(StackComponent.Count));
        }

        private void OnStackInteractUsing(EntityUid uid, StackComponent stack, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(args.Used, out StackComponent? recipientStack))
                return;

            var localRotation = Transform(args.Used).LocalRotation;

            if (!TryMergeStacks(uid, args.Used, out var transfered, stack, recipientStack))
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

            switch (transfered)
            {
                case > 0:
                    Popup.PopupCoordinates($"+{transfered}", popupPos, Filter.Local(), false);

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

        private bool TryMergeStacks(
            EntityUid donor,
            EntityUid recipient,
            out int transferred,
            StackComponent? donorStack = null,
            StackComponent? recipientStack = null)
        {
            transferred = 0;
            if (donor == recipient)
                return false;

            if (!Resolve(recipient, ref recipientStack, false) || !Resolve(donor, ref donorStack, false))
                return false;

            if (string.IsNullOrEmpty(recipientStack.StackTypeId) || !recipientStack.StackTypeId.Equals(donorStack.StackTypeId))
                return false;

            transferred = Math.Min(donorStack.Count, GetAvailableSpace(recipientStack));
            SetCount(donor, donorStack.Count - transferred, donorStack);
            SetCount(recipient, recipientStack.Count + transferred, recipientStack);
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
                TryMergeStacks(item, held, out _, donorStack: itemStack);

                if (itemStack.Count == 0)
                    return;
            }

            Hands.PickupOrDrop(user, item, handsComp: hands);
        }

        public virtual void SetCount(EntityUid uid, int amount, StackComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            // Do nothing if amount is already the same.
            if (amount == component.Count)
                return;

            // Store old value for event-raising purposes...
            var old = component.Count;

            // Clamp the value.
            amount = Math.Min(amount, GetMaxCount(component));
            amount = Math.Max(amount, 0);

            // Server-side override deletes the entity if count == 0
            component.Count = amount;
            Dirty(uid, component);

            Appearance.SetData(uid, StackVisuals.Actual, component.Count);
            RaiseLocalEvent(uid, new StackCountChangedEvent(old, component.Count));
        }

        /// <summary>
        ///     Try to use an amount of items on this stack. Returns whether this succeeded.
        /// </summary>
        public bool Use(EntityUid uid, int amount, StackComponent? stack = null)
        {
            if (!Resolve(uid, ref stack))
                return false;

            // Check if we have enough things in the stack for this...
            if (stack.Count < amount)
            {
                // Not enough things in the stack, return false.
                return false;
            }

            // We do have enough things in the stack, so remove them and change.
            if (!stack.Unlimited)
            {
                SetCount(uid, stack.Count - amount, stack);
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

                if (!TryMergeStacks(uid, otherEnt, out _, stack, otherStack))
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
            entProto.TryGetComponent<StackComponent>(out var stackComp, EntityManager.ComponentFactory);
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

            SetCount(targetEnt, targetStack.Count + change, targetStack);
            SetCount(insertEnt, insertStack.Count - change, insertStack);
            return true;
        }

        private void OnStackStarted(EntityUid uid, StackComponent component, ComponentStartup args)
        {
            // on client, lingering stacks that start at 0 need to be darkened
            // on server this does nothing
            SetCount(uid, component.Count, component);

            if (!TryComp(uid, out AppearanceComponent? appearance))
                return;

            Appearance.SetData(uid, StackVisuals.Actual, component.Count, appearance);
            Appearance.SetData(uid, StackVisuals.MaxCount, GetMaxCount(component), appearance);
            Appearance.SetData(uid, StackVisuals.Hide, false, appearance);
        }

        private void OnStackGetState(EntityUid uid, StackComponent component, ref ComponentGetState args)
        {
            args.State = new StackComponentState(component.Count, component.MaxCountOverride, component.Lingering);
        }

        private void OnStackHandleState(EntityUid uid, StackComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not StackComponentState cast)
                return;

            component.MaxCountOverride = cast.MaxCount;
            component.Lingering = cast.Lingering;
            // This will change the count and call events.
            SetCount(uid, cast.Count, component);
        }

        private void OnStackExamined(EntityUid uid, StackComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.PushMarkup(
                Loc.GetString("comp-stack-examine-detail-count",
                    ("count", component.Count),
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
}
