using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Printer;

[Serializable, NetSerializable]
public enum PrinterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PrinterPrintFile : BoundUserInterfaceMessage
{
    public string Content;
    public int Number;
    public string Name;

    public PrinterPrintFile(string content, int number, string name)
    {
        Content = content;
        Number = number;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class PrinterPaperStuck : EntityEventArgs
{

    [NonSerialized]
    public EntityUid Printer;

    public PrinterPaperStuck(EntityUid uid)
    {
        Printer = uid;
    }
    
}

[Serializable, NetSerializable]
public sealed class PrinterPaperInteractStuck : EntityEventArgs
{

    [NonSerialized]
    public EntityUid Printer;
    
    public PrinterPaperInteractStuck(EntityUid uid)
    {
        Printer = uid;
    }

}

[Serializable, NetSerializable]
public sealed partial class PrinterUnstuckEvent : SimpleDoAfterEvent
{}