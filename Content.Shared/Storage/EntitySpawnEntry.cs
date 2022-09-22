using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Storage;

/// <summary>
///     Dictates a list of items that can be spawned.
/// </summary>
[Serializable]
[DataDefinition]
public struct EntitySpawnEntry
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? PrototypeId = null;

    /// <summary>
    ///     The probability that an item will spawn. Takes decimal form so 0.05 is 5%, 0.50 is 50% etc.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prob")] public float SpawnProbability = 1;

    /// <summary>
    ///     orGroup signifies to pick between entities designated with an ID.
    ///     <example>
    ///         <para>
    ///             To define an orGroup in a StorageFill component you
    ///             need to add it to the entities you want to choose between and
    ///             add a prob field. In this example there is a 50% chance the storage
    ///             spawns with Y or Z.
    ///         </para>
    ///         <code>
    /// - type: StorageFill
    ///   contents:
    ///     - name: X
    ///     - name: Y
    ///       prob: 0.50
    ///       orGroup: YOrZ
    ///     - name: Z
    ///       orGroup: YOrZ
    /// </code>
    ///     </example>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("orGroup")] public string? GroupId = null;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("amount")] public int Amount = 1;

    /// <summary>
    ///     How many of this can be spawned, in total.
    ///     If this is lesser or equal to <see cref="Amount"/>, it will spawn <see cref="Amount"/> exactly.
    ///     Otherwise, it chooses a random value between <see cref="Amount"/> and <see cref="MaxAmount"/> on spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxAmount")] public int MaxAmount = 1;

    public EntitySpawnEntry() { }
}

public static class EntitySpawnCollection
{
    private sealed class OrGroup
    {
        public List<EntitySpawnEntry> Entries { get; set; } = new();
        public float CumulativeProbability { get; set; } = 0f;
    }

    /// <summary>
    ///     Using a collection of entity spawn entries, picks a random list of entity prototypes to spawn from that collection.
    /// </summary>
    /// <remarks>
    ///     This does not spawn the entities. The caller is responsible for doing so, since it may want to do something
    ///     special to those entities (offset them, insert them into storage, etc)
    /// </remarks>
    /// <param name="entries">The entity spawn entries.</param>
    /// <param name="random">Resolve param.</param>
    /// <returns>A list of entity prototypes that should be spawned.</returns>
    public static List<string?> GetSpawns(IEnumerable<EntitySpawnEntry> entries,
        IRobustRandom? random = null)
    {
        IoCManager.Resolve(ref random);

        var spawned = new List<string?>();
        var orGroupedSpawns = new Dictionary<string, OrGroup>();

        // collect groups together, create singular items that pass probability
        foreach (var entry in entries)
        {
            // Handle "Or" groups
            if (!string.IsNullOrEmpty(entry.GroupId))
            {
                if (!orGroupedSpawns.TryGetValue(entry.GroupId, out OrGroup? orGroup))
                {
                    orGroup = new();
                    orGroupedSpawns.Add(entry.GroupId, orGroup);
                }

                orGroup.Entries.Add(entry);
                orGroup.CumulativeProbability += entry.SpawnProbability;
                continue;
            }

            // else
            // Check random spawn
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (entry.SpawnProbability != 1f && !random.Prob(entry.SpawnProbability)) continue;

            var amount = entry.Amount;

            if (entry.MaxAmount > amount)
                amount = random.Next(amount, entry.MaxAmount);

            for (var i = 0; i < amount; i++)
            {
                spawned.Add(entry.PrototypeId);
            }
        }

        // handle orgroup spawns
        foreach (var spawnValue in orGroupedSpawns.Values)
        {
            // For each group use the added cumulative probability to roll a double in that range
            double diceRoll = random.NextDouble() * spawnValue.CumulativeProbability;
            // Add the entry's spawn probability to this value, if equals or lower, spawn item, otherwise continue to next item.
            var cumulative = 0.0;
            foreach (var entry in spawnValue.Entries)
            {
                cumulative += entry.SpawnProbability;
                if (diceRoll > cumulative) continue;
                // Dice roll succeeded, add item and break loop

                var amount = entry.Amount;

                if (entry.MaxAmount > amount)
                    amount = random.Next(amount, entry.MaxAmount);

                for (var index = 0; index < amount; index++)
                {
                    spawned.Add(entry.PrototypeId);
                }

                break;
            }
        }

        return spawned;
    }
}
