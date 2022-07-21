using System.Threading;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed class ForensicScannerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// A list of fingerprint GUIDs that the forensic scanner found from the <see cref="ForensicsComponent"/> on an entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public List<string> Fingerprints = new();
        /// <summary>
        /// A list of glove fibers that the forensic scanner found from the <see cref="ForensicsComponent"/> on an entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public List<string> Fibers = new();

        /// <summary>
        /// The time (in seconds) that it takes to scan an entity.
        /// </summary>
        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;
    }
}
