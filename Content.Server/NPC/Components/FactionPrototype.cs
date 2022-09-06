using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.NPC.Components
{
    /// <summary>
    /// Contains data about this faction's relations with other factions.
    /// </summary>
    [Prototype("faction")]
    public sealed class FactionPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField("friendly", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<FactionPrototype>))]
        public HashSet<string> Friendly = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("hostile", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<FactionPrototype>))]
        public HashSet<string> Hostile = new();
    }
}
