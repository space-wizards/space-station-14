using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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

        [DataField("researchDiskReward", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: false)]
        public string ResearchDiskReward = string.Empty;
    }
}
