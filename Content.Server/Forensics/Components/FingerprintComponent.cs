namespace Content.Server.Forensics
{
    /// <summary>
    /// This component is for mobs that leave fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed class FingerprintComponent : Component
    {
        [DataField("fingerprint")]
        public string? Fingerprint;
    }
}
