using Content.Shared.Storage.Components;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    private void OnStorageFillMapInit(EntityUid uid, StorageFillComponent component, MapInitEvent args)
    {
        if (component.Contents.Count == 0)
            return;

        TryComp<StorageComponent>(uid, out var storageComp);
        SharedEntityStorageComponent? entityStorageComp = null;
        _entityStorage.ResolveStorage(uid, ref entityStorageComp);

        if (entityStorageComp == null && storageComp == null)
        {
            Log.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, _random);
        foreach (var item in spawnItems)
        {
            var ent = EntityManager.SpawnEntity(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (entityStorageComp != null && _entityStorage.Insert(ent, uid))
                continue;

            if (storageComp != null && Insert(uid, ent, storageComp, false))
                continue;

            Log.Error($"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't.");
            EntityManager.DeleteEntity(ent);
        }
    }
}
