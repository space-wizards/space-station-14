using Content.Server.Spawners.Components;
using Content.Server.Storage.Components;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
            // No, you are not allowed to fill a container with entity spawners.
            DebugTools.Assert(!_prototype.Index<EntityPrototype>(item)
                .HasComponent(typeof(RandomSpawnerComponent)));
            var ent = EntityManager.SpawnEntity(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (entityStorageComp != null && EntityStorage.Insert(ent, uid, entityStorageComp))
                continue;

            var reason = string.Empty;
            if (storageComp != null && Insert(uid, ent, out _, out reason, storageComp: storageComp, playSound: false))
                continue;

            Log.Error($"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't. Reason: {Loc.GetString(reason ?? "no reason.")}");
            EntityManager.DeleteEntity(ent);
        }
    }
}
