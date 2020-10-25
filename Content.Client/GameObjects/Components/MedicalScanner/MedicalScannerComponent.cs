using Content.Shared.GameObjects.Components.Medical;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public class MedicalScannerComponent : SharedMedicalScannerComponent
    {
    }
}
