using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;

namespace Content.Shared.Stacks
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedStackComponent, ComponentHandleState>(OnStackHandleState);
            SubscribeLocalEvent<SharedStackComponent, ComponentStartup>(OnStackStarted);
            SubscribeLocalEvent<SharedStackComponent, ExaminedEvent>(OnStackExamined);
        }

        private void OnStackStarted(EntityUid uid, SharedStackComponent component, ComponentStartup args)
        {
            if (!ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
                return;

            appearance.SetData(StackVisuals.Actual, component.Count);
            appearance.SetData(StackVisuals.MaxCount, component.MaxCount);
            appearance.SetData(StackVisuals.Hide, false);
        }

        public void SetCount(EntityUid uid, SharedStackComponent component, int amount)
        {
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
            component.Dirty();

            // Queue delete stack if count reaches zero.
            if(component.Count <= 0)
                EntityManager.QueueDeleteEntity(uid);

            // Change appearance data.
            if (ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
                appearance.SetData(StackVisuals.Actual, component.Count);

            RaiseLocalEvent(uid, new StackCountChangedEvent(old, component.Count));
        }

        private void OnStackHandleState(EntityUid uid, SharedStackComponent component, ComponentHandleState args)
        {
            if (args.Current is not StackComponentState cast)
                return;

            // This will change the count and call events.
            SetCount(uid, component, cast.Count);
            component.MaxCount = cast.MaxCount;
        }

        private void OnStackExamined(EntityUid uid, SharedStackComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.Message.AddText("\n");
            args.Message.AddMarkup(
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
