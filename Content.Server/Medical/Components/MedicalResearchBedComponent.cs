using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;

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
    }
}
