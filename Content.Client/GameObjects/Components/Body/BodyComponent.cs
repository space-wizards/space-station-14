#nullable enable
using Content.Client.GameObjects.Components.Disposal;
using Content.Client.GameObjects.Components.MedicalScanner;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IDraggable
    {
        public bool CanDrop(CanDropEventArgs eventArgs)
        {
            if (eventArgs.Target.HasComponent<DisposalUnitComponent>() ||
                eventArgs.Target.HasComponent<MedicalScannerComponent>())
            {
                return true;
            }

            return false;
        }
    }
}
