using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.IoC;

namespace Content.Server.StationEvents.Components
{
    [RegisterComponent, Access(typeof(VentCrittersRule))]
    public sealed partial class VentCrittersRuleComponent : Component
    {
        public List<EntitySpawnEntry> Entries = new();

        /// <summary>
        /// At least one special entry is guaranteed to spawn
        /// </summary>
        public List<EntitySpawnEntry> SpecialEntries = new();

        /// <summary>
        /// Defines the maximum number of critters that can be spawned
        /// </summary>
        public int CritterCapacity { get; set; } = 10; // Default capacity is 10 critters

        // List of holopad spawn points
        public List<EntityUid> HolopadSpawnPoints { get; set; } = new();

        // Method to check if the capacity has been reached
        public bool CanSpawnMoreCritters(int currentCritterCount)
        {
            return currentCritterCount < CritterCapacity;
        }

        // Method to spawn a specific special entity on holopads
        public void SpawnSpecialEntityOnHolopads(EntitySpawnEntry specialEntity, int numberToSpawn)
        {
            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var entitySystem = entitySystemManager.GetEntitySystem<EntitySpawnSystem>();

            int spawnedCount = 0;
            foreach (var holopad in HolopadSpawnPoints)
            {
                for (int i = 0; i < numberToSpawn; i++)
                {
                    // Actual spawn logic
                    entitySystem.SpawnEntity(specialEntity.EntityId, holopad.Transform.Coordinates);

                    spawnedCount++;
                    if (spawnedCount >= numberToSpawn)
                    {
                        break;
                    }
                }

                if (spawnedCount >= numberToSpawn)
                {
                    break;
                }
            }
        }

        // Method to spawn multiple instances of a specific entity on holopads
        public void SpawnMultipleSpecialEntitiesOnHolopads(string entityId, int numberToSpawn)
        {
            foreach (var specialEntry in SpecialEntries)
            {
                if (specialEntry.EntityId == entityId)
                {
                    SpawnSpecialEntityOnHolopads(specialEntry, numberToSpawn);
                    break; // Assuming you only need to spawn a specific number of this entity
                }
            }
        }
    }
}

public sealed partial class EntitySpawnEntry
{
    public string EntityId { get; set; } = default!;
    public int SpawnCount { get; set; } = 1;
    public EntitySpawnLocation SpawnLocation { get; set; } = default!;
}

public sealed partial class EntitySpawnLocation
{
    public string TargetEntityId { get; set; } = default!;
    public bool IsOnTop { get; set; } = true;
}

