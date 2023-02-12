using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.Disease;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
        public float ScanDelay = 0.8f;
        /// <summary>
        ///     Token for interrupting scanning do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);

        /// <summary>
        ///     Sound played on scanning begin
        /// </summary>
        [DataField("scanningBeginSound")]
        public SoundSpecifier? ScanningBeginSound = null;

        /// <summary>
        ///     Sound played on scanning end
        /// </summary>
        [DataField("scanningEndSound")]
        public SoundSpecifier? ScanningEndSound = null;

        /// <summary>
        /// The disease this will give people.
        /// </summary>
        [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? Disease;
    }
}
