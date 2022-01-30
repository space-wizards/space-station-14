using System.Collections.Generic;
using Content.Server.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Random;

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
        var alreadySpawnedGroups = new HashSet<string>();

        foreach (var entry in component.Contents)
        {
            // Handle "Or" groups
            if (!string.IsNullOrEmpty(entry.GroupId) && alreadySpawnedGroups.Contains(entry.GroupId)) continue;

            // Check random spawn
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (entry.SpawnProbability != 1f && !_random.Prob(entry.SpawnProbability)) continue;

            for (var i = 0; i < entry.Amount; i++)
            {
                var ent = EntityManager.SpawnEntity(entry.PrototypeId, coordinates);

                if (storage.Insert(ent)) continue;

                Logger.ErrorS("storage", $"Tried to StorageFill {entry.PrototypeId} inside {uid} but can't.");
                EntityManager.DeleteEntity(ent);
            }

            if (!string.IsNullOrEmpty(entry.GroupId)) alreadySpawnedGroups.Add(entry.GroupId);
        }
    }
}
