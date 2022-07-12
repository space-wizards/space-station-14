using Content.Server.Storage.Components;
using Content.Shared.Storage;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem
{
    private void OnStorageFillMapInit(EntityUid uid, StorageFillComponent component, MapInitEvent args)
    {
        if (component.Contents.Count == 0) return;
        // ServerStorageComponent needs to rejoin IStorageComponent when other storage components are ECS'd
        TryComp<IStorageComponent>(uid, out var storage);
        TryComp<ServerStorageComponent>(uid, out var serverStorageComp);
        if (storage == null && serverStorageComp == null)
        {
            Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, _random);
        foreach (var item in spawnItems)
        {
            var ent = EntityManager.SpawnEntity(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (storage != null && storage.Insert(ent))
               continue;

            if (serverStorageComp != null && Insert(uid, ent, serverStorageComp))
                continue;

            Logger.ErrorS("storage", $"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't.");
            EntityManager.DeleteEntity(ent);
        }
    }
}
