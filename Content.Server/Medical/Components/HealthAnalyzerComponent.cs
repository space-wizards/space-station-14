using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.Components
{
    /// <summary>
    ///    After scanning, retrieves the target Uid to use with its related UI.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedHealthAnalyzerComponent))]
    public sealed class HealthAnalyzerComponent : SharedHealthAnalyzerComponent
    {
        /// <summary>
        /// How long it takes to scan someone.
        /// </summary>
        [DataField("scanDelay")]
        [ViewVariables]
        public float ScanDelay = 0.8f;
        /// <summary>
        ///     Token for interrupting scanning do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);
    }
}
