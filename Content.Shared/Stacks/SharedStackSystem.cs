using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Stacks
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
        [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
        [Dependency] protected readonly SharedHandsSystem HandsSystem = default!;
        [Dependency] protected readonly SharedTransformSystem Xform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IViewVariablesManager _vvm = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedStackComponent, ComponentGetState>(OnStackGetState);
            SubscribeLocalEvent<SharedStackComponent, ComponentHandleState>(OnStackHandleState);
            SubscribeLocalEvent<SharedStackComponent, ComponentStartup>(OnStackStarted);
            SubscribeLocalEvent<SharedStackComponent, ExaminedEvent>(OnStackExamined);
            SubscribeLocalEvent<SharedStackComponent, InteractUsingEvent>(OnStackInteractUsing);

            _vvm.GetTypeHandler<SharedStackComponent>()
                .AddPath(nameof(SharedStackComponent.Count), (_, comp) => comp.Count, SetCount);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _vvm.GetTypeHandler<SharedStackComponent>()
                .RemovePath(nameof(SharedStackComponent.Count));
        }

        private void OnStackInteractUsing(EntityUid uid, SharedStackComponent stack, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(args.Used, out SharedStackComponent? recipientStack))
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
                    PopupSystem.PopupCoordinates($"+{transfered}", popupPos, Filter.Local());

                    if (recipientStack.AvailableSpace == 0)
                    {
                        PopupSystem.PopupCoordinates(Loc.GetString("comp-stack-becomes-full"),
                            popupPos.Offset(new Vector2(0, -0.5f)), Filter.Local());
                    }

                    break;

                case 0 when recipientStack.AvailableSpace == 0:
                    PopupSystem.PopupCoordinates(Loc.GetString("comp-stack-already-full"), popupPos, Filter.Local());
                    break;
            }
        }

        private bool TryMergeStacks(
            EntityUid donor,
            EntityUid recipient,
            out int transfered,
            SharedStackComponent? donorStack = null,
            SharedStackComponent? recipientStack = null)
        {
            transfered = 0;
            if (!Resolve(recipient, ref recipientStack, false) || !Resolve(donor, ref donorStack, false))
                return false;

            if (!recipientStack.StackTypeId.Equals(donorStack.StackTypeId))
                return false;

            transfered = Math.Min(donorStack.Count, recipientStack.AvailableSpace);
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
            SharedStackComponent? itemStack = null,
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

        public virtual void SetCount(EntityUid uid, int amount, SharedStackComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            // Do nothing if amount is already the same.
            if (amount == component.Count)
                return;

            // Store old value for event-raising purposes...
            var old = component.Count;

            // Clamp the value.
            if (amount > component.MaxCount)
            {
                amount = component.MaxCount;
            }

            if (amount < 0)
            {
                amount = 0;
            }

            component.Count = amount;
            Dirty(component);

            Appearance.SetData(uid, StackVisuals.Actual, component.Count);
            RaiseLocalEvent(uid, new StackCountChangedEvent(old, component.Count), false);
        }

        /// <summary>
        ///     Try to use an amount of items on this stack. Returns whether this succeeded.
        /// </summary>
        public bool Use(EntityUid uid, int amount, SharedStackComponent? stack = null)
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

        private void OnStackStarted(EntityUid uid, SharedStackComponent component, ComponentStartup args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearance))
                return;

            Appearance.SetData(uid, StackVisuals.Actual, component.Count, appearance);
            Appearance.SetData(uid, StackVisuals.MaxCount, component.MaxCount, appearance);
            Appearance.SetData(uid, StackVisuals.Hide, false, appearance);
        }

        private void OnStackGetState(EntityUid uid, SharedStackComponent component, ref ComponentGetState args)
        {
            args.State = new StackComponentState(component.Count, component.MaxCount);
        }

        private void OnStackHandleState(EntityUid uid, SharedStackComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not StackComponentState cast)
                return;

            component.MaxCount = cast.MaxCount;
            // This will change the count and call events.
            SetCount(uid, cast.Count, component);
        }

        private void OnStackExamined(EntityUid uid, SharedStackComponent component, ExaminedEvent args)
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
        public int OldCount { get; }

        /// <summary>
        ///     The new stack count.
        /// </summary>
        public int NewCount { get; }

        public StackCountChangedEvent(int oldCount, int newCount)
        {
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
