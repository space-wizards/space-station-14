using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            SubscribeLocalEvent<ItemMapperComponent, ComponentInit>(InitLayers);
            SubscribeLocalEvent<ItemMapperComponent, EntInsertedIntoContainerMessage>(MapperEntityInserted);
            SubscribeLocalEvent<ItemMapperComponent, EntRemovedFromContainerMessage>(MapperEntityRemoved);
            SubscribeLocalEvent<ItemCounterComponent, EntInsertedIntoContainerMessage>(CounterEntityInserted);
            SubscribeLocalEvent<ItemCounterComponent, EntRemovedFromContainerMessage>(CounterEntityRemoved);
        }

        private void InitLayers(EntityUid uid, ItemMapperComponent component, ComponentInit args)
        {
            if (component.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent))
            {
                var list = new List<string>(component.MapLayers.Keys);
                appearanceComponent.SetData(StorageMapVisuals.InitLayers, new ShowLayerData(list));
            }
        }

        private void MapperEntityRemoved(EntityUid uid, ItemMapperComponent itemMapper,
            EntRemovedFromContainerMessage args)
        {
            if (itemMapper.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetLayers(args, itemMapper, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        private void MapperEntityInserted(EntityUid uid, ItemMapperComponent itemMapper,
            EntInsertedIntoContainerMessage args)
        {
            if (itemMapper.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetLayers(args, itemMapper, out var containedLayers))
            {
                appearanceComponent.SetData(StorageMapVisuals.LayerChanged, new ShowLayerData(containedLayers));
            }
        }

        private void CounterEntityInserted(EntityUid uid, ItemCounterComponent itemCounter,
            EntInsertedIntoContainerMessage args)
        {
            if (itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetCount(args, itemCounter, out var count))
            {
                appearanceComponent.SetData(StackVisuals.Actual, count);
                if (itemCounter.MaxAmount != null)
                    appearanceComponent.SetData(StackVisuals.MaxCount, itemCounter.MaxAmount);
            }
        }

        private void CounterEntityRemoved(EntityUid uid, ItemCounterComponent itemCounter,
            EntRemovedFromContainerMessage args)
        {
            if (itemCounter.Owner.TryGetComponent(out SharedAppearanceComponent? appearanceComponent)
                && TryGetCount(args, itemCounter, out var count))
            {
                appearanceComponent.SetData(StackVisuals.Actual, count);
                if (itemCounter.MaxAmount != null)
                    appearanceComponent.SetData(StackVisuals.MaxCount, itemCounter.MaxAmount);
            }
        }

        protected abstract bool TryGetLayers(ContainerModifiedMessage msg,
            ItemMapperComponent itemMapper,
            out IReadOnlyList<string> containedLayers);

        protected abstract bool TryGetCount(ContainerModifiedMessage msg,
            ItemCounterComponent itemCounter, [NotNullWhen(true)] out int? count);
    }
}
