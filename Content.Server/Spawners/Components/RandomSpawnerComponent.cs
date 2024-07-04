using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class RandomSpawnerComponent : ConditionalSpawnerComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> RarePrototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float RareChance { get; set; } = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Offset { get; set; } = 0.2f;

        [DataField]
        public bool DeleteSpawnerAfterSpawn = true;
    }
}
