using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed partial class HolosignProjectorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SignProto = "HolosignWetFloor";

        /// <summary>
        /// How much charge a single use expends.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("chargeUse")]
        public float ChargeUse = 50f;
    }
}
