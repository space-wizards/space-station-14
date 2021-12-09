using System;
using System.Collections.Generic;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    public sealed class StorageFillComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "StorageFill";

        [DataField("contents")] private List<EntitySpawnEntry> _contents = new();

        public IReadOnlyList<EntitySpawnEntry> Contents => _contents;

        void IMapInit.MapInit()
        {
            if (_contents.Count == 0)
            {
                return;
            }

            if (!_entMan.TryGetComponent(Owner, out IStorageComponent? storage))
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();

            var alreadySpawnedGroups = new List<string>();
            foreach (var storageItem in _contents)
            {
                if (!string.IsNullOrEmpty(storageItem.GroupId) &&
                    alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

				var entMan = _entMan;
				var transform = entMan.GetComponent<TransformComponent>(Owner);

                for (var i = 0; i < storageItem.Amount; i++)
                {

                    var ent = entMan.SpawnEntity(storageItem.PrototypeId, transform.Coordinates);

                    if (storage.Insert(ent)) continue;

                    Logger.ErrorS("storage", $"Tried to StorageFill {storageItem.PrototypeId} inside {Owner} but can't.");
                    entMan.DeleteEntity(ent);
                }

                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }
        }
    }
}
