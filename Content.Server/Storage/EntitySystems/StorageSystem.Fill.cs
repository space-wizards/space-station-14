using Content.Server.Storage.Components;
using Robust.Shared.Random;
using System.Linq;

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

        var orGroupedSpawns = new Dictionary<string, List<EntitySpawnEntry>>();

        // collect groups together, create singular items that pass probability
        foreach (var entry in component.Contents)
        {
            // Handle "Or" groups
            if (!string.IsNullOrEmpty(entry.GroupId))
            {
                if (!orGroupedSpawns.ContainsKey(entry.GroupId))
                {
                    List<EntitySpawnEntry> currentGroup = new();
                    currentGroup.Add(entry);
                    orGroupedSpawns.Add(entry.GroupId, currentGroup);
                    continue;
                }
                orGroupedSpawns[entry.GroupId].Add(entry);
                continue;
            }

            // else
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
        }

        // handle orgroup spawns
        foreach (var group in orGroupedSpawns)
        {
            Random r = new Random();
            double diceRoll = r.NextDouble();
            List<EntitySpawnEntry> shuffled = group.Value.OrderBy(a => r.Next()).ToList();

            double cumulative = 0.0;
            for (int i = 0; i < shuffled.Count; i++)
            {
                cumulative += shuffled[i].SpawnProbability;
                if (diceRoll < cumulative)
                {
                    for (var index = 0; index < shuffled[i].Amount; index++)
                    {
                        var ent = EntityManager.SpawnEntity(shuffled[i].PrototypeId, coordinates);

                        if (storage.Insert(ent)) continue;

                        Logger.ErrorS("storage", $"Tried to StorageFill {shuffled[i].PrototypeId} inside {uid} but can't.");
                        EntityManager.DeleteEntity(ent);
                    }
                    break;
                }
            }
        }
    }
}
