using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent, ComponentProtoName("CablePlacer")]
    public sealed class CablePlacerComponent : Component
    {
        [ViewVariables]
        [DataField("cablePrototypeID", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? CablePrototypeId = "CableHV";

        [ViewVariables]
        [DataField("blockingWireType")]
        public CableType BlockingCableType = CableType.HighVoltage;
    }
}
