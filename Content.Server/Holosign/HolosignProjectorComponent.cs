using Robust.Shared.Prototypes;

namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed partial class HolosignProjectorComponent : Component
    {
        [DataField]
        public EntProtoId SignProto = "HolosignWetFloor";

        /// <summary>
        /// How much charge a single use expends.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("chargeUse")]
        public float ChargeUse = 50f;
    }
}
