using Robust.Shared.Serialization;

namespace Content.Shared.Forensics
{
    [Serializable, NetSerializable]
    public sealed class ForensicScannerUserMessage : BoundUserInterfaceMessage
    {
        public readonly List<string> Fingerprints = new();
        public readonly List<string> Fibers = new();

        public ForensicScannerUserMessage(List<string> fingerprints, List<string> fibers)
        {
            Fingerprints = fingerprints;
            Fibers = fibers;
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
}
