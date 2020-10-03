#nullable enable
using Content.Client.GameObjects.Components.Disposal;
using Content.Client.GameObjects.Components.MedicalScanner;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IClientDraggable
    {
        public bool ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            if (eventArgs.Target.HasComponent<DisposalUnitComponent>() ||
                eventArgs.Target.HasComponent<MedicalScannerComponent>())
            {
                return true;
            }

            return false;
        }

        public bool ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
