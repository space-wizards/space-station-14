using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Storage;

namespace Content.Server._Impstation.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class SpawnEntrySpawnerComponent : Component, ISerializationHooks
    {
        /// <summary>
        /// List of entities that can be spawned by this component. Each entity will be spawned according to the rules of entity spawn entries.
        /// </summary>
        [DataField]
        public List<EntitySpawnEntry>? Spawns;

        /// <summary>
        /// Scatter of entity spawn coordinates
        /// </summary>
        [DataField]
        public float Range { get; set; } = 0.2f;
    }
}
