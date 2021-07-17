
using System.Collections.Generic;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Storage.Components
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
    public class StorageCounterComponent : Component, ISerializationHooks
    {
        // TODO Convert to EntityWhitelist
        [DataField("countTag")]
        private string? _countTag;

        [DataField("amount")]
        private int? _maxAmount;

        /// <summary>
        /// Single item storage component usually have an attached StackedVisualizer.
        /// </summary>
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default;

        public override string Name => "StorageCounter";

        void ISerializationHooks.AfterDeserialization()
        {
            if (_countTag == null)
            {
                Logger.Warning("StorageCounterComponent without a `countTag` is useless");
            }
        }

        public void ContainerUpdateAppearance(IContainer container)
        {
            if(_appearanceComponent is null)
                return;

            var actual = Count(container.ContainedEntities);
            _appearanceComponent.SetData(StackVisuals.Actual, actual);

            if (_maxAmount != null)
            {
                _appearanceComponent.SetData(StackVisuals.MaxCount, _maxAmount);
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
