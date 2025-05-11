using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Forensics
{
    [Serializable, NetSerializable]
    public sealed class ForensicScannerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly Dictionary<ProtoId<ForensicEvidencePrototype>, List<string>> Evidence = [];
        public readonly List<string> CleaningAgents = [];
        public readonly string LastScannedName = string.Empty;
        public readonly TimeSpan PrintCooldown = TimeSpan.Zero;
        public readonly TimeSpan PrintReadyAt = TimeSpan.Zero;

        public ForensicScannerBoundUserInterfaceState(
            Dictionary<ProtoId<ForensicEvidencePrototype>, List<string>> evidence,
            List<string> cleaningAgents,
            string lastScannedName,
            TimeSpan printCooldown,
            TimeSpan printReadyAt)
        {
            Evidence = evidence;
            CleaningAgents = cleaningAgents;
            LastScannedName = lastScannedName;
            PrintCooldown = printCooldown;
            PrintReadyAt = printReadyAt;
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
