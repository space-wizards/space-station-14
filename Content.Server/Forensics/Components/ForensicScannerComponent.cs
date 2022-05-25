using System.Threading;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed class ForensicScannerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        public List<string> Fingerprints = new();
        public List<string> Fibers = new();

        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;
    }
}
