using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Holocuff
{
    [RegisterComponent]
    public sealed partial class HolocuffProjectorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CuffProto = "HolocuffSecurity";

        /// <summary>
        /// How much charge a single use expends.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("chargeUse")]
        public float ChargeUse = 50f;

        [ViewVariables(VVAccess.ReadWrite), DataField("virtualCuff")]
        public EntityUid? VirtualCuff;
    }
}
