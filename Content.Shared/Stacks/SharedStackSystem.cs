using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stacks
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
        [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
        [Dependency] protected readonly SharedHandsSystem HandsSystem = default!;
        [Dependency] protected readonly SharedTransformSystem Xform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IViewVariablesManager _vvm = default!;

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

            if (!TryMergeStacks(uid, args.Used, out var transfered, stack, recipientStack))
                return;

            args.Handled = true;

            // interaction is done, the rest is just generating a pop-up

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            var popupPos = args.ClickLocation;

            if (!popupPos.IsValid(EntityManager))
            {
                popupPos = Transform(args.User).Coordinates;
            }

            switch (transfered)
            {
                case > 0:
                    PopupSystem.PopupCoordinates($"+{transfered}", popupPos, Filter.Local(), false);

                    if (GetAvailableSpace(recipientStack) == 0)
                    {
                        PopupSystem.PopupCoordinates(Loc.GetString("comp-stack-becomes-full"),
                            popupPos.Offset(new Vector2(0, -0.5f)), Filter.Local(), false);
                    }

                    break;

                case 0 when GetAvailableSpace(recipientStack) == 0:
                    PopupSystem.PopupCoordinates(Loc.GetString("comp-stack-already-full"), popupPos, Filter.Local(), false);
                    break;
            }
        }

        private bool TryMergeStacks(
            EntityUid donor,
            EntityUid recipient,
            out int transfered,
            StackComponent? donorStack = null,
            StackComponent? recipientStack = null)
        {
            transfered = 0;
            if (!Resolve(recipient, ref recipientStack, false) || !Resolve(donor, ref donorStack, false))
                return false;

            if (recipientStack.StackTypeId == null || !recipientStack.StackTypeId.Equals(donorStack.StackTypeId))
                return false;

            transfered = Math.Min(donorStack.Count, GetAvailableSpace(recipientStack));
            SetCount(donor, donorStack.Count - transfered, donorStack);
            SetCount(recipient, recipientStack.Count + transfered, recipientStack);
            return true;
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
            SharedHandsComponent? hands = null)
        {
            if (!Resolve(user, ref hands, false))
                return;

            if (!Resolve(item, ref itemStack, false))
            {
                // This isn't even a stack. Just try to pickup as normal.
                HandsSystem.PickupOrDrop(user, item, handsComp: hands);
                return;
            }

            // This is shit code until hands get fixed and give an easy way to enumerate over items, starting with the currently active item.
            foreach (var held in HandsSystem.EnumerateHeld(user, hands))
            {
                TryMergeStacks(item, held, out _, donorStack: itemStack);

                if (itemStack.Count == 0)
                    return;
            }

            HandsSystem.PickupOrDrop(user, item, handsComp: hands);
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

            component.Count = amount;
            Dirty(component);

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

            if (component.StackTypeId == null)
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

        private void OnStackStarted(EntityUid uid, StackComponent component, ComponentStartup args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearance))
                return;

            Appearance.SetData(uid, StackVisuals.Actual, component.Count, appearance);
            Appearance.SetData(uid, StackVisuals.MaxCount, GetMaxCount(component), appearance);
            Appearance.SetData(uid, StackVisuals.Hide, false, appearance);
        }

        private void OnStackGetState(EntityUid uid, StackComponent component, ref ComponentGetState args)
        {
            args.State = new StackComponentState(component.Count, GetMaxCount(component));
        }

        private void OnStackHandleState(EntityUid uid, StackComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not StackComponentState cast)
                return;

            component.MaxCountOverride = cast.MaxCount;
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
