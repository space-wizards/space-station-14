using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedItemCounterSystem : EntitySystem
    {
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
            if (!itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)) return;
            
            var count = GetCount(args, itemCounter);
            if (count == null)
                return;
            
            appearanceComponent.SetData(StackVisuals.Actual, count);
            if (itemCounter.MaxAmount != null)
                appearanceComponent.SetData(StackVisuals.MaxCount, itemCounter.MaxAmount);
            
        }

        private void CounterEntityRemoved(EntityUid uid, ItemCounterComponent itemCounter,
            EntRemovedFromContainerMessage args)
        {
            if (!itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)) return;
            
            var count = GetCount(args, itemCounter);
            if (count == null)
                return;
                
            appearanceComponent.SetData(StackVisuals.Actual, count);
            if (itemCounter.MaxAmount != null)
                appearanceComponent.SetData(StackVisuals.MaxCount, itemCounter.MaxAmount);
        }
        
        protected abstract int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter);
    }
}