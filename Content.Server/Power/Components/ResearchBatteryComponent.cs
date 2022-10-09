namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    public class ResearchBatteryComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("researchMode")]
        public bool researchMode = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("analysedCharge")]
        public float analysedCharge = 0;

        public float lastRecordedCharge;

    }
}
