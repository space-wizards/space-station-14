using Robust.Shared.Serialization;

namespace Content.Shared.Forensics;

[Serializable, NetSerializable]
public enum ForensicScannerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ForensicScannerPrintMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ForensicScannerClearMessage : BoundUserInterfaceMessage;
