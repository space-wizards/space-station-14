using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public sealed partial class RandomSpawnerComponent : ConditionalSpawnerComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rarePrototypes", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> RarePrototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rareChance")]
        public float RareChance { get; set; } = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offset")]
        public float Offset { get; set; } = 0.2f;
    }
}
