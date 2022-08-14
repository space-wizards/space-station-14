using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    [Access(typeof(GasMixerSystem))]
    public sealed class GasMixerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOne")]
        public string InletOneName = "inletOne";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwo")]
        public string InletTwoName = "inletTwo";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("targetPressure")]
        public float TargetPressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTargetPressure")]
        public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletOneConcentration")]
        public float InletOneConcentration = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inletTwoConcentration")]
        public float InletTwoConcentration = 0.5f;

        [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string OnPort = "On";

        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string OffPort = "Off";

        [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string TogglePort = "Toggle";
    }
}
