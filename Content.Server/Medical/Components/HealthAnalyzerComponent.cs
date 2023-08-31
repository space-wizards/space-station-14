using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Medical.Components
{
    /// <summary>
    ///    After scanning, retrieves the target Uid to use with its related UI.
    /// </summary>
    [RegisterComponent]
    public sealed partial class HealthAnalyzerComponent : Component
    {
        /// <summary>
        /// How long it takes to scan someone.
        /// </summary>
        [DataField("scanDelay")]
        public float ScanDelay = 0.8f;

        public PlayerBoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);

        /// <summary>
        ///     Sound played on scanning begin
        /// </summary>
        [DataField("scanningBeginSound")]
        public SoundSpecifier? ScanningBeginSound;

        /// <summary>
        ///     Sound played on scanning end
        /// </summary>
        [DataField("scanningEndSound")]
        public SoundSpecifier? ScanningEndSound;
    }
}
