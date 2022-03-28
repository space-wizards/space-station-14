using Content.Server.Storage.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Storage;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem
{
    private void OnStorageFillMapInit(EntityUid uid, StorageFillComponent component, MapInitEvent args)
    {
        if (component.Contents.Count == 0) return;

        if (!TryComp<IStorageComponent>(uid, out var storage))
        {
            Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, _random);
        foreach (var item in spawnItems)
        {
            var ent = EntityManager.SpawnEntity(item, coordinates);

            if (storage.Insert(ent)) continue;

            Logger.ErrorS("storage", $"Tried to StorageFill {item} inside {uid} but can't.");
            EntityManager.DeleteEntity(ent);
        }
    }
}
