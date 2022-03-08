using Content.Server.UserInterface;
using Content.Shared.DragDrop;
using Content.Shared.MedicalScanner;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Medical.Components
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
