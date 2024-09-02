using System.Linq;
using Content.Server.Spawners.Components;
using Content.Server.Storage.Components;
using Content.Shared.Item;
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

        if (TryComp<StorageComponent>(uid, out var storageComp))
        {
            FillStorage((uid, component, storageComp));
        }
        else if (TryComp<EntityStorageComponent>(uid, out var entityStorageComp))
        {
            FillEntityStorage((uid, component, entityStorageComp));
        }
        else
        {
            Log.Error($"StorageFillComponent couldn't find any StorageComponent ({uid})");
        }
    }

    private void FillStorage(Entity<StorageFillComponent?, StorageComponent?> entity)
    {
        var (uid, component, storage) = entity;

        if (!Resolve(uid, ref component, ref storage))
            return;

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, Random);

        var items = new List<Entity<ItemComponent>>();
        foreach (var spawnPrototype in spawnItems)
        {
            var ent = Spawn(spawnPrototype, coordinates);

            // No, you are not allowed to fill a container with entity spawners.
            DebugTools.Assert(!_prototype.Index<EntityPrototype>(spawnPrototype)
                .HasComponent(typeof(RandomSpawnerComponent)));

            if (!TryComp<ItemComponent>(ent, out var itemComp))
            {
                Log.Error($"Tried to fill {ToPrettyString(entity)} with non-item {spawnPrototype}.");
                Del(ent);
                continue;
            }

            items.Add((ent, itemComp));
        }

        // we order the items from biggest to smallest to try and reduce poor placement in the grid.
        var sortedItems = items
            .OrderByDescending(x => ItemSystem.GetItemShape(x.Comp).GetArea());

        ClearCantFillReasons();
        foreach (var ent in sortedItems)
        {
            if (Insert(uid, ent, out _, out var reason, storageComp: storage, playSound: false))
                continue;

            if (CantFillReasons.Count > 0)
            {
                var reasons = string.Join(", ", CantFillReasons.Select(s => Loc.GetString(s)));
                if (reason == null)
                    reason = reasons;
                else
                    reason += $", {reasons}";
            }

            Log.Error($"Tried to StorageFill {ToPrettyString(ent)} inside {ToPrettyString(uid)} but can't. reason: {reason}");
            ClearCantFillReasons();
            Del(ent);
        }
    }

    private void FillEntityStorage(Entity<StorageFillComponent?, EntityStorageComponent?> entity)
    {
        var (uid, component, entityStorageComp) = entity;

        if (!Resolve(uid, ref component, ref entityStorageComp))
            return;

        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(component.Contents, Random);
        foreach (var item in spawnItems)
        {
            // No, you are not allowed to fill a container with entity spawners.
            DebugTools.Assert(!_prototype.Index<EntityPrototype>(item)
                .HasComponent(typeof(RandomSpawnerComponent)));
            var ent = Spawn(item, coordinates);

            // handle depending on storage component, again this should be unified after ECS
            if (entityStorageComp != null && EntityStorage.Insert(ent, uid, entityStorageComp))
                continue;

            Log.Error($"Tried to StorageFill {item} inside {ToPrettyString(uid)} but can't.");
            Del(ent);
        }
    }
}
