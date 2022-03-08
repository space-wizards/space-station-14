using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner
{
    public abstract class SharedMedicalScannerComponent : Component, IDragDropOn
    {
        [Serializable, NetSerializable]
        public sealed class MedicalScannerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool IsScannable;

            public MedicalScannerBoundUserInterfaceState(bool isScannable)
            {
                IsScannable = isScannable;
            }
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerStatus
        {
            Off,
            Open,
            Red,
            Death,
            Green,
            Yellow,
        }

        [Serializable, NetSerializable]
        public sealed class ScanButtonPressedMessage : BoundUserInterfaceMessage
        {
        }

        public bool CanInsert(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(entity);
        }

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
