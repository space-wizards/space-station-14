using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    internal sealed class StorageFillComponent : Component, IMapInit
    {
        public override string Name => "StorageFill";

        private List<PrototypeItemData> _contents;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _contents, "contents", new List<PrototypeItemData>());
        }

        void IMapInit.MapInit()
        {
            if (_contents.Count == 0)
            {
                return;
            }

            if (!Owner.TryGetComponent(out IStorageComponent storage))
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }
            var random = IoCManager.Resolve<IRobustRandom>();

            var alreadySpawnedGroups = new List<string>();
            foreach (var storageItem in _contents)
            {
                if (string.IsNullOrEmpty(storageItem.PrototypeName)) continue;
                if (!string.IsNullOrEmpty(storageItem.GroupId) && alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                for (var i = 0; i < storageItem.Amount; i++)
                {
                    storage.Insert(Owner.EntityManager.SpawnEntity(storageItem.PrototypeName, Owner.Transform.Coordinates));
                }
                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }
        }

        [Serializable]
        private struct PrototypeItemData : IExposeData
        {
            public string PrototypeName;
            public float SpawnProbability;
            public string GroupId;
            public int Amount;

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref PrototypeName, "name", null);
                serializer.DataField(ref Amount, "amount", 1);
                serializer.DataField(ref SpawnProbability, "prob", 1f);
                serializer.DataField(ref GroupId, "orGroup", null);
            }
        }
    }
}
