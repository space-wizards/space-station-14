using Content.Server.UserInterface;
using Content.Shared.HealthAnalyzer;
using Robust.Server.GameObjects;

namespace Content.Server.HealthAnalyzer
{
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
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);
    }
}
