using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent, EntityCategory("Spawner")]
    [Virtual]
    public partial class ConditionalSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public List<EntProtoId> GameRules = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public float Chance { get; set; } = 1.0f;
    }
}
