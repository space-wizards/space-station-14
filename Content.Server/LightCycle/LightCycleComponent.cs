namespace Content.Server.LightCycle
{
    [RegisterComponent]
    public sealed partial class LightCycleComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("isEnabled")]
        public bool IsEnabled = false;
        [ViewVariables(VVAccess.ReadWrite), DataField("isColorEnabled")]
        public bool IsColorShiftEnabled = false;
        [ViewVariables(VVAccess.ReadOnly)]
        public double CurrentTime = 0;
        [ViewVariables(VVAccess.ReadWrite), DataField("initialTime")]
        public double InitialTime = 400;
        [ViewVariables(VVAccess.ReadWrite), DataField("cycleDuration")]
        public double CycleDuration = 1200;
        [ViewVariables(VVAccess.ReadWrite), DataField("minLightLevel")]
        public double MinLightLevel = 0.25;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxLightLevel")]
        public double MaxLightLevel = 1.25;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipLight")]
        public double ClipLight = 1.25;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipRed")]
        public double ClipRed = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipGreen")]
        public double ClipGreen = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipBlue")]
        public double ClipBlue = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("minRedLevel")]
        public double MinRedLevel = 0.125;
        [ViewVariables(VVAccess.ReadWrite), DataField("minGreenLevel")]
        public double MinGreenLevel = 0.2;
        [ViewVariables(VVAccess.ReadWrite), DataField("minBlueLevel")]
        public double MinBlueLevel = 0.50;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxRedLevel")]
        public double MaxRedLevel = 2;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxGreenLevel")]
        public double MaxGreenLevel = 2;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxBlueLevel")]
        public double MaxBlueLevel = 5;
    }
}
