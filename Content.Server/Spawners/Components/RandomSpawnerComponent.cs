using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class RandomSpawnerComponent : ConditionalSpawnerComponent
    {
        /// <summary>
        /// A list of rarer entities that can spawn with the RareChance
        /// instead of one of the entities in the Prototypes list.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> RarePrototypes { get; set; } = new();

        /// <summary>
        /// The chance that a rare prototype may spawn instead of a common prototype
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float RareChance { get; set; } = 0.05f;

        /// <summary>
        /// Scatter of entity spawn coordinates
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Offset { get; set; } = 0.2f;

        /// <summary>
        /// A variable meaning whether the spawn will
        /// be able to be used again or whether
        /// it will be destroyed after the first use
        /// </summary>
        [DataField]
        public bool DeleteSpawnerAfterSpawn = true;
    }
}
