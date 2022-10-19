using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.NPC.Components
{
    /// <summary>
    /// Contains data about this faction's relations with other factions.
    /// </summary>
    [Prototype("faction")]
    public readonly record struct FactionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("friendly", customTypeSerializer:typeof(PrototypeIdListSerializer<FactionPrototype>))]
        public readonly List<string> Friendly = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("hostile", customTypeSerializer:typeof(PrototypeIdListSerializer<FactionPrototype>))]
        public readonly List<string> Hostile = new();
    }
}
