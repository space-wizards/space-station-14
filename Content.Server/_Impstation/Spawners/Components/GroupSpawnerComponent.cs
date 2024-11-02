using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Storage;

namespace Content.Server._Impstation.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class GroupSpawnerComponent : Component, ISerializationHooks
    {
        /// <summary>
        /// List of entities that can be spawned by this component. Each entity will be spawned once.
        /// </summary>
        [DataField]
        public List<EntitySpawnEntry?> Spawns = [];
    }
}
