using Content.Shared.DragDrop;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner
{
    public abstract partial class SharedMedicalScannerComponent : Component
    {
        [Serializable, NetSerializable]
        public enum MedicalScannerVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerStatus : byte
        {
            Off,
            Open,
            Red,
            Death,
            Green,
            Yellow,
        }
    }
}
