using Robust.Shared.Serialization;

namespace Content.Shared.Printer;

[Serializable, NetSerializable]
public enum PrinterVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum PrinterVisualState : byte
{
    Empty,
    Paper,
    Crumpled,
    CrumpledPaper,
    PrintingLast,
    Printing
}
