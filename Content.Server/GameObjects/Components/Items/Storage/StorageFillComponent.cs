#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    public sealed class StorageFillComponent : Component, IMapInit
    {
        public override string Name => "StorageFill";

        [DataField("contents")]
        private List<StorageFillEntry> _contents = new();

        public IReadOnlyList<StorageFillEntry> Contents => _contents;

        void IMapInit.MapInit()
        {
            if (_contents.Count == 0)
            {
                return;
            }

            if (!Owner.TryGetComponent(out IStorageComponent? storage))
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();

            var alreadySpawnedGroups = new List<string>();
            foreach (var storageItem in _contents)
            {
                if (string.IsNullOrEmpty(storageItem.PrototypeId)) continue;
                if (!string.IsNullOrEmpty(storageItem.GroupId) && alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                for (var i = 0; i < storageItem.Amount; i++)
                {
                    storage.Insert(Owner.EntityManager.SpawnEntity(storageItem.PrototypeId, Owner.Transform.Coordinates));
                }
                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }
        }

        [Serializable]
        [DataDefinition]
        public struct StorageFillEntry : IPopulateDefaultValues
        {
            [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
            public string? PrototypeId;

            [DataField("prob")]
            public float SpawnProbability;
            /// <summary>
            /// The probability that an item will spawn. Takes decimal form so 0.05 is 5%, 0.50 is 50% etc.
            /// </summary>
            [DataField("orGroup")]
            public string GroupId;
            /// <summary>
            /// orGroup signifies to pick between entities designated with an ID.
            ///
            /// <example>
            /// <para>To define an orGroup in a StorageFill component you
            /// need to add it to the entities you want to choose between and
            /// add a prob field. In this example there is a 50% chance the storage
            /// spawns with Y or Z.
            /// 
            /// </para>
            /// <code>
            /// - type: StorageFill
            ///   contents: 
            ///     - name: X
            ///     - name: Y
            ///       prob: 0.50
            ///       orGroup: YOrZ
            ///     - name: Z
            ///       orGroup: YOrZ
            /// </code>
            /// </example> 
            /// </summary>
            [DataField("amount")]
            public int Amount;

            public void PopulateDefaultValues()
            {
                Amount = 1;
                SpawnProbability = 1;
            }
        }
    }
}
