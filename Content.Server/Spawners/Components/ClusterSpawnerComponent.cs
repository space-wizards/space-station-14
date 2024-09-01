using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class ClusterSpawnerComponent : Component, ISerializationHooks
    {
        /// <summary>
        /// List of entities that can be spawned by this component. One will be randomly
        /// chosen for each entity spawned. When multiple entities are spawned at once,
        /// each will be randomly chosen separately.
        /// </summary>
        [DataField]
        public List<EntProtoId> Prototypes = [];

        /// <summary>
        /// Chance of spawning an entity
        /// </summary>
        [DataField]
        public float Chance = 1.0f;

        /// <summary>
        /// The minimum number of entities that can be spawned.
        /// </summary>
        [DataField]
        public int MinimumEntitiesSpawned = 1;

        /// <summary>
        /// The maximum number of entities that can be spawned.
        /// </summary>
        [DataField]
        public int MaximumEntitiesSpawned = 1;

        void ISerializationHooks.AfterDeserialization()
        {
            if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }
    }
}
