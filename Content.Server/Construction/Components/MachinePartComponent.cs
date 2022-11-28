using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed class MachinePartComponent : Component
    {
        [DataField("part", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string PartType { get; private set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rating")]
        public int Rating { get; private set; } = 1;
    }
}
