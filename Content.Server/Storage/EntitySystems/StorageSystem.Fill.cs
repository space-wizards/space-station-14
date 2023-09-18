using Content.Server.Storage.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem
{
    private void OnStorageFillMapInit(EntityUid uid, StorageFillComponent component, MapInitEvent args)
    {
        if (component.Contents.Count == 0)
            return;

        TryComp<StorageComponent>(uid, out var storageComp);
        TryComp<EntityStorageComponent>(uid, out var entityStorageComp);

        if (entityStorageComp == null && storageComp == null)
        {
            Log.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, Random);
        foreach (var item in spawnItems)
        {
            var ent = EntityManager.SpawnEntity(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (entityStorageComp != null && EntityStorage.Insert(ent, uid))
                continue;

            if (storageComp != null && Insert(uid, ent, out _, storageComp: storageComp, playSound: false))
                continue;

            Log.Error($"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't.");
            EntityManager.DeleteEntity(ent);
        }
    }
}
