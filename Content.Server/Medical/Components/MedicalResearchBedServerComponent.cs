using Content.Shared.Medical;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public sealed class MedicalResearchBedServerComponent : Component
    {

        //note - this was originally going to be a server shared between serverl beds per station, but for several reasons this was abandoned
        //it may be worth consolidating this with the main MedicalResearchBedComponent
        /// <summary>
        /// The health changes this server has recorded.
        /// </summary>

        public bool bedChange = true;
        public bool diskPrinted = false;
        public FixedPoint2 lastHealthRecording = 0f;
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 healthChanges = 0f;
    }
}
