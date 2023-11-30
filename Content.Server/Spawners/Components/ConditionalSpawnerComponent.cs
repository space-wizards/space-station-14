using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    [Virtual]
    public partial class ConditionalSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototypes", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gameRules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> GameRules = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chance")]
        public float Chance { get; set; } = 1.0f;
    }
}
