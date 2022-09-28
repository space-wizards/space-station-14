using Content.Shared.Medical;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public sealed class MedicalResearchBedServerComponent : Component
    {
        /// <summary>
        /// The health changes this server has recorded.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]

        public bool bedChange = true;

        public FixedPoint2 lastHealthRecording = 0f;
        public FixedPoint2 healthChanges = 0f;
    }
}
