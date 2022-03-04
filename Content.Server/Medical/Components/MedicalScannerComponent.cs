using Content.Shared.DragDrop;
using Content.Shared.MedicalScanner;
using Robust.Shared.Containers;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.MedicalScanner
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMedicalScannerComponent))]
    public sealed class MedicalScannerComponent : SharedMedicalScannerComponent
    {
        public ContainerSlot BodyContainer = default!;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(MedicalScannerUiKey.Key);

        // ECS this out!, when DragDropSystem and InteractionSystem refactored
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
