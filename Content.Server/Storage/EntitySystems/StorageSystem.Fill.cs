using Content.Server.Spawners.Components;
using Content.Server.Storage.Components;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem
{
    private void OnStorageFillMapInit(EntityUid uid, StorageFillComponent component, MapInitEvent args)
    {
        if (component.Contents.Count == 0)
            return;

        TryComp<ServerStorageComponent>(uid, out var serverStorageComp);
        TryComp<EntityStorageComponent>(uid, out var entityStorageComp);

        if (entityStorageComp == null && serverStorageComp == null)
        {
            Log.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, _random);
        foreach (var item in spawnItems)
        {
            // No, you are not allowed to fill a container with entity spawners.
            DebugTools.Assert(!_prototype.Index<EntityPrototype>(item)
                    .HasComponent(typeof(RandomSpawnerComponent)));

            var ent = EntityManager.SpawnEntity(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (_entityStorage.Insert(ent, uid))
               continue;

            if (Insert(uid, ent, serverStorageComp, false))
                continue;

            Log.Error($"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't.");
            EntityManager.DeleteEntity(ent);
        }
    }
}
