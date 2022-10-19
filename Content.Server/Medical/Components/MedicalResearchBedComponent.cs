using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Components
{
    /// <summary>
    /// After interact retrieves the target Uid to use with its related UI.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedMedicalResearchBedComponent))]
    public sealed class MedicalResearchBedComponent : SharedMedicalResearchBedComponent
    {
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(MedicalResearchBedUiKey.Key);

        [ViewVariables]
        [DataField("healthGoal")]
        public int HealthGoal = 0;

        [ViewVariables]
        [DataField("researchDiskReward")]
        public string ResearchDiskReward = string.Empty;

        public bool bedChange = true;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool diskPrinted = false;
        public FixedPoint2 lastHealthRecording = 0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 healthChanges = 0f;
    }
}
