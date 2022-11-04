using System.Threading;
using Robust.Shared.Audio;

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

        public string LastScanned = string.Empty;

        /// <summary>
        /// The time (in seconds) that it takes to scan an entity.
        /// </summary>
        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;

        /// <summary>
        /// The sound that's played when there's a match between a scan and an
        /// inserted forensic pad.
        /// </summary>
        [DataField("soundMatch")]
        public SoundSpecifier SoundMatch = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

        /// <summary>
        /// The sound that's played when there's no match between a scan and an
        /// inserted forensic pad.
        /// </summary>
        [DataField("soundNoMatch")]
        public SoundSpecifier SoundNoMatch = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
    }
}
