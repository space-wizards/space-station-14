using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Stacks
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedStackComponent, ComponentStartup>(OnStackStarted);
            SubscribeLocalEvent<SharedStackComponent, StackChangeCountEvent>(OnStackCountChange);
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

        protected void OnStackCountChange(EntityUid uid, SharedStackComponent component, StackChangeCountEvent args)
        {
            if (args.Amount == component.Count)
                return;

            var old = component.Count;

            if (args.Amount > component.MaxCount)
            {
                args.Amount = component.MaxCount;
                args.Clamped = true;
            }

            if (args.Amount < 0)
            {
                args.Amount = 0;
                args.Clamped = true;
            }

            component.Count = args.Amount;
            component.Dirty();

            // Queue delete stack if count reaches zero.
            if(component.Count <= 0)
                EntityManager.QueueDeleteEntity(uid);

            // Change appearance data.
            if (ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
                appearance.SetData(StackVisuals.Actual, component.Count);

            RaiseLocalEvent(uid, new StackCountChangedEvent(old, component.Count));
        }

        private void OnStackExamined(EntityUid uid, SharedStackComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.Message.AddText("\n");
            args.Message.AddMarkup(
                Robust.Shared.Localization.Loc.GetString("comp-stack-examine-detail-count",
                    ("count", component.Count),
                    ("markupCountColor", "lightgray")
                )
            );
        }
    }

    /// <summary>
    ///     Attempts to change the amount of things in a stack to a specific number.
    ///     If the amount had to be clamped to zero or the max amount, <see cref="Clamped"/> will be true
    ///     and the amount will be changed to match the value set.
    ///     Does nothing if the amount is the same as the stack count already.
    /// </summary>
    public class StackChangeCountEvent : EntityEventArgs
    {
        /// <summary>
        ///     Amount to set the stack to.
        ///     Input/Output parameter.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        ///     Whether the <see cref="Amount"/> had to be clamped.
        ///     Output parameter.
        /// </summary>
        public bool Clamped { get; set; }

        public StackChangeCountEvent(int amount)
        {
            Amount = amount;
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
