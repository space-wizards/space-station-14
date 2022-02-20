using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.PlayingCard
{
    [UsedImplicitly]
    public abstract class SharedPlayingCardSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            // SubscribeLocalEvent<SharedPlayingCardComponent, ComponentGetState>(OnStackGetState);
            // SubscribeLocalEvent<SharedPlayingCardComponent, ComponentHandleState>(OnStackHandleState);
            // SubscribeLocalEvent<SharedPlayingCardComponent, ComponentStartup>(OnStackStarted);
            // SubscribeLocalEvent<SharedPlayingCardComponent, ExaminedEvent>(OnStackExamined);
        }

        // public void SetCount(EntityUid uid, int amount, SharedPlayingCardComponent? component = null)
        // {
        //     if (!Resolve(uid, ref component))
        //         return;

        //     // Do nothing if amount is already the same.
        //     if (amount == component.Count)
        //         return;

        //     // Store old value for event-raising purposes...
        //     var old = component.Count;

        //     if (amount < 0)
        //     {
        //         amount = 0;
        //     }

        //     component.Count = amount;
        //     component.Dirty();

        //     // Queue delete stack if count reaches zero.
        //     if(component.Count <= 0)
        //         QueueDel(uid);

        //     // Change appearance data.
        //     if (TryComp(uid, out AppearanceComponent? appearance))
        //         appearance.SetData(PlayingCardVisuals.Actual, component.Count);

        //     RaiseLocalEvent(uid, new StackCountChangedEvent(old, component.Count), false);
        // }

        /// <summary>
        ///     Try to use an amount of items on this stack. Returns whether this succeeded.
        /// </summary>
        // public bool Use(EntityUid uid, int amount, SharedPlayingCardComponent? stack = null)
        // {
        //     if (!Resolve(uid, ref stack))
        //         return false;

        //     // Check if we have enough things in the stack for this...
        //     if (stack.Count < amount)
        //     {
        //         // Not enough things in the stack, return false.
        //         return false;
        //     }

        //     // We do have enough things in the stack, so remove them and change.

        //     SetCount(uid, stack.Count - amount, stack);

        //     return true;
        // }

        // private void OnStackStarted(EntityUid uid, SharedPlayingCardComponent component, ComponentStartup args)
        // {
        //     if (!TryComp(uid, out AppearanceComponent? appearance))
        //         return;

        //     appearance.SetData(PlayingCardVisuals.Actual, component.Count);
        //     appearance.SetData(PlayingCardVisuals.MaxCount, component.MaxCount);
        //     appearance.SetData(PlayingCardVisuals.Hide, false);
        // }

        // private void OnStackGetState(EntityUid uid, SharedPlayingCardComponent component, ref ComponentGetState args)
        // {
        //     args.State = new PlayingCardComponentState(component.Count, component.MaxCount);
        // }

        // private void OnStackHandleState(EntityUid uid, SharedPlayingCardComponent component, ref ComponentHandleState args)
        // {
        //     if (args.Current is not PlayingCardComponentState cast)
        //         return;

        //     component.MaxCount = cast.MaxCount;
        //     // This will change the count and call events.
        //     SetCount(uid, cast.Count, component);
        // }

        // private void OnStackExamined(EntityUid uid, SharedPlayingCardComponent component, ExaminedEvent args)
        // {
        //     if (!args.IsInDetailsRange)
        //         return;

        //     args.PushMarkup(
        //         Loc.GetString("comp-stack-examine-detail-count",
        //             ("count", component.Count),
        //             ("markupCountColor", "lightgray")
        //         )
        //     );
        // }
    }

    /// <summary>
    ///     Event raised when a stack's count has changed.
    /// </summary>
    public class StackCountChangedEvent : EntityEventArgs
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
