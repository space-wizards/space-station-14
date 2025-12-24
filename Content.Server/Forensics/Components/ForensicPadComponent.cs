namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to take a sample of someone's fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ForensicPadComponent : Component
    {
        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;

        public bool Used = false;
        public String Sample = string.Empty;

        // What it can take a sample of.
        [DataField]
        public bool Fingerprint = false;
        [DataField]
        public bool Fiber = false;
        [DataField]
        public bool Reagent = false;
        [DataField]
        public bool ReagentContraband = false;
    }
}
