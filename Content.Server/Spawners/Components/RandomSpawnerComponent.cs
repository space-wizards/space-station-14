using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public sealed class RandomSpawnerComponent : ConditionalSpawnerComponent
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
