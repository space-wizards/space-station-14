using Content.Shared.DragDrop;
using Content.Shared.MedicalScanner;
using Robust.Shared.Containers;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public sealed class MedicalScannerComponent : SharedMedicalScannerComponent
    {
        public ContainerSlot BodyContainer = default!;

        public EntityUid? ConnectedConsole;

        /// <summary>
        ///     The port for medical scanners.
        /// </summary>
        [DataField("scannerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string ScannerPort = "MedicalScannerReceiver";

        // ECS this out!, when DragDropSystem and InteractionSystem refactored
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }
    }
}
