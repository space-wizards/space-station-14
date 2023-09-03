using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.NPC.Components
{
    /// <summary>
    /// Contains data about this faction's relations with other factions.
    /// </summary>
    [Prototype("npcFaction")]
    public sealed class NpcFactionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("friendly", customTypeSerializer:typeof(PrototypeIdListSerializer<NpcFactionPrototype>))]
        public List<string> Friendly = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("hostile", customTypeSerializer:typeof(PrototypeIdListSerializer<NpcFactionPrototype>))]
        public List<string> Hostile = new();
    }
}
