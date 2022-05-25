using System.Threading;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed class ForensicScannerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [ViewVariables(VVAccess.ReadOnly)]
        public List<string> Fingerprints = new();
        [ViewVariables(VVAccess.ReadOnly)]
        public List<string> Fibers = new();

        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;
    }
}
