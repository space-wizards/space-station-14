using Content.Shared.DragDrop;
using Content.Shared.MedicalScanner;
using Robust.Shared.GameObjects;

namespace Content.Client.MedicalScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public sealed class MedicalScannerComponent : SharedMedicalScannerComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
