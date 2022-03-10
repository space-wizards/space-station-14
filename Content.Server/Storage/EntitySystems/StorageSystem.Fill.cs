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

        var orGroupedSpawns = new Dictionary<string, OrGroup>();

        // collect groups together, create singular items that pass probability
        foreach (var entry in component.Contents)
        {
            // Handle "Or" groups
            if (!string.IsNullOrEmpty(entry.GroupId))
            {
                if (!orGroupedSpawns.TryGetValue(entry.GroupId, out OrGroup? orGroup))
                {
                    orGroup = new(new List<EntitySpawnEntry>(), 0f);
                    orGroupedSpawns.Add(entry.GroupId, orGroup);
                }
                orGroup.Entries.Add(entry);
                orGroup.CumulativeProbability += entry.SpawnProbability;
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
            double diceRoll = _random.NextDouble() * group.Value.CumulativeProbability;
            _random.Shuffle(group.Value.Entries);

            double cumulative = 0.0;
            for (int i = 0; i < group.Value.Entries.Count; i++)
            {
                cumulative += group.Value.Entries[i].SpawnProbability;
                if (diceRoll <= cumulative)
                {
                    for (var index = 0; index < group.Value.Entries[i].Amount; index++)
                    {
                        var ent = EntityManager.SpawnEntity(group.Value.Entries[i].PrototypeId, coordinates);

                        if (storage.Insert(ent)) continue;

                        Logger.ErrorS("storage", $"Tried to StorageFill {group.Value.Entries[i].PrototypeId} inside {uid} but can't.");
                        EntityManager.DeleteEntity(ent);
                    }
                    break;
                }
            }
        }
    }
    private sealed class OrGroup
    {
        public List<EntitySpawnEntry> Entries { get; set; } = new();
        public float CumulativeProbability { get; set; } = 0f;
        public OrGroup(List<EntitySpawnEntry> entries, float cumulativeProbability)
        {
            Entries = entries;
            CumulativeProbability = cumulativeProbability;
        }
    }
}
