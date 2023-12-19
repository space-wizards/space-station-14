using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components
{
    [NetworkedComponent, RegisterComponent]
    [AutoGenerateComponentState]
    public sealed partial class LightCycleComponent : Component
    {

        public string? OriginalColor;

        [AutoNetworkedField]
        [DataField("offset")]
        public TimeSpan Offset;

        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("isEnabled")]
        public bool IsEnabled = true;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("initialTime")]
        public int InitialTime = 600;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("cycleDuration")]
        public int CycleDuration = 1800;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("minLightLevel")]
        public double MinLightLevel = 0.2;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("maxLightLevel")]
        public double MaxLightLevel = 1.25;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("clipLight")]
        public double ClipLight = 1.25;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("clipRed")]
        public double ClipRed = 1;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("clipGreen")]
        public double ClipGreen = 1;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("clipBlue")]
        public double ClipBlue = 1.25;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("minRedLevel")]
        public double MinRedLevel = 0.1;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("minGreenLevel")]
        public double MinGreenLevel = 0.15;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("minBlueLevel")]
        public double MinBlueLevel = 0.50;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("maxRedLevel")]
        public double MaxRedLevel = 2;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("maxGreenLevel")]
        public double MaxGreenLevel = 2;
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite), DataField("maxBlueLevel")]
        public double MaxBlueLevel = 5;
    }
}
