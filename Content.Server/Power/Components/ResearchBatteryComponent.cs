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
        public float analysedCharge = 0f;

        /// <summary>
        ///     Can be toggled but will switch off if not enough charge on the next cycle.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("shieldingActive")]
        public bool shieldingActive = false;

        /// <summary>
        ///     Cost of shielding per analysis cycle relative to the MaxAnalysisCharge.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("shieldingCost")]
        public float shieldingCost = 0.3f;

        /// <summary>
        ///     How much charge is siphoned per change relative to that charge. This can be reconfigured via interface.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("analysisSiphon")]
        public float analysisSiphon = 1f;

        /// <summary>
        ///     The analysis charge is not capable of exceeding this amount.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxAnalysisCharge = 10000000f;

        /// <summary>
        ///     The research battery component will not increase a batteries maxcap any more than this amount.
        /// </summary>
        public float MaxChargeCeiling = 100000000f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float researchGoal = 20000000f;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool researchAchieved = false;

        /// <summary>
        ///     The amount of analysis charge discharged per analysis cycle. Relative to the analysis charge itself.
        /// </summary>
        public float AnalysisDischarge = 0.2f;

        /// <summary>
        ///     The cap increase for the battery per analysis cycle. Relative to the analysis charge.
        /// </summary>
        public float CapIncrease = 0.1f;

        /// <summary>
        ///     If the charge exceeds this amout, relative to the MaxAnalysis charge, without shielding, the SMES will take damage equal to the excess charge
        ///     divided by 100000 per cycle
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("overloadThreshold")]
        public float overloadThreshold = 0.75f;

        public float lastRecordedCharge;

    }
}
