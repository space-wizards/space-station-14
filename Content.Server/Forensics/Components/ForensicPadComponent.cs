using System.Threading;

namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to take a sample of someone's fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed class ForensicPadComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;

        public bool Used = false;
        public String Sample = string.Empty;
    }
}
