using Robust.Shared.Serialization;

namespace Content.Shared.Forensics
{
    [Serializable, NetSerializable]
    public sealed class ForensicScannerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly List<string> Fingerprints = new();
        public readonly List<string> Fibers = new();
        public readonly string LastScannedName = string.Empty;

        public ForensicScannerBoundUserInterfaceState(List<string> fingerprints, List<string> fibers, string lastScannedName)
        {
            Fingerprints = fingerprints;
            Fibers = fibers;
            LastScannedName = lastScannedName;
        }
    }

    [Serializable, NetSerializable]
    public enum ForensicScannerUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class ForensicScannerPrintMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class ForensicScannerClearMessage : BoundUserInterfaceMessage
    {
    }
}
