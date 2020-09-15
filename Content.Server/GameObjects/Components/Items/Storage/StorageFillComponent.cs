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

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    internal sealed class StorageFillComponent : Component, IMapInit
    {
        public override string Name => "StorageFill";

        private List<PrototypeItemData> _contents = new List<PrototypeItemData>();

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

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

            var storage = Owner.GetComponent<IStorageComponent>();
            var random = IoCManager.Resolve<IRobustRandom>();

            void Spawn(string prototype)
            {
                storage.Insert(_entityManager.SpawnEntity(prototype, Owner.Transform.Coordinates));
            }

            var alreadySpawnedGroups = new List<string>();
            foreach (var storageItem in _contents)
            {
                if (storageItem.PrototypeName == null) continue;
                if (storageItem.GroupId != null && alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (!(Math.Abs(storageItem.SpawnProbability - 1f) < 0.001f) &&
                    !random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                Spawn(storageItem.PrototypeName);
                if(storageItem.GroupId != null) alreadySpawnedGroups.Add(storageItem.GroupId);
            }
        }

        [Serializable]
        protected struct PrototypeItemData : IExposeData
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
