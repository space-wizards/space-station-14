using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    [Virtual]
    public partial class ConditionalSpawnerComponent : Component
    {
        /// <summary>
        /// A list of entities, one of which can spawn in after calling Spawn()
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> Prototypes { get; set; } = new();

        /// <summary>
        /// A list of game rules.
        /// If at least one of them was launched in the game,
        /// an attempt will occur to spawn one of the objects in the Prototypes list
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> GameRules = new();

        /// <summary>
        /// Chance of spawning an entity
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Chance { get; set; } = 1.0f;
    }
}
