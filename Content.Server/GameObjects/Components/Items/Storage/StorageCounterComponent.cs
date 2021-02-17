#nullable enable

using System.Collections.Generic;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    /// <summary>
    /// Storage that spawns and counts a single item.
    /// Usually used for things like matchboxes, cigarette packs,
    /// cigar cases etc.
    /// </summary>
    /// <code>
    ///  - type: StorageCounter
    ///    amount: 6 # Note: this field can be omitted
    ///    countTag: Cigarette # Note: field doesn't point to entity Id, but its tag
    /// </code>
    [RegisterComponent]
    public class StorageCounterComponent : Component
    {
        private string? _countTag;
        private int? _maxAmount;

        /// <summary>
        /// Single item storage component usually have an attached StackedVisualizer.
        /// </summary>
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default;

        public override string Name => "StorageCounter";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _countTag, "countTag", null);
            if (_countTag == null)
            {
                Logger.Warning("StorageCounterComponent without a `countTag` is useless");
            }
            serializer.DataField(ref _maxAmount, "amount", null);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (_appearanceComponent != null)
            {
                switch (message)
                {
                    case ContainerContentsModifiedMessage msg:
                        var actual = Count(msg.Container.ContainedEntities);
                        _appearanceComponent.SetData(StackVisuals.Actual, actual);
                        if (_maxAmount != null)
                        {
                            _appearanceComponent.SetData(StackVisuals.MaxCount, _maxAmount);
                        }

                        break;
                }
            }
        }

        private int Count(IReadOnlyList<IEntity> containerContainedEntities)
        {
            var count = 0;
            if (_countTag != null)
            {
                foreach (var entity in containerContainedEntities)
                {
                    if (entity.HasTag(_countTag))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
