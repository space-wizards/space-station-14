using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedItemCounterSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ItemCounterComponent, EntInsertedIntoContainerMessage>(CounterEntityInserted);
            SubscribeLocalEvent<ItemCounterComponent, EntRemovedFromContainerMessage>(CounterEntityRemoved);
        }

        private void CounterEntityInserted(EntityUid uid, ItemCounterComponent itemCounter,
            EntInsertedIntoContainerMessage args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
                return;

            var count = GetCount(args, itemCounter);
            if (count == null)
                return;

            _appearance.SetData(uid, StackVisuals.Actual, count, appearanceComponent);

            if (itemCounter.MaxAmount != null)
                _appearance.SetData(uid, StackVisuals.MaxCount, itemCounter.MaxAmount, appearanceComponent);
        }

        private void CounterEntityRemoved(EntityUid uid, ItemCounterComponent itemCounter,
            EntRemovedFromContainerMessage args)
        {
            if (!TryComp(uid, out AppearanceComponent? appearanceComponent))
                return;

            var count = GetCount(args, itemCounter);
            if (count == null)
                return;

            _appearance.SetData(uid, StackVisuals.Actual, count, appearanceComponent);
            if (itemCounter.MaxAmount != null)
                _appearance.SetData(uid, StackVisuals.MaxCount, itemCounter.MaxAmount, appearanceComponent);
        }

        protected abstract int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter);
    }
}
