using Content.Shared.Printer;
using Robust.Server.GameObjects;

namespace Content.Server.Printer;

[RegisterComponent]
public sealed partial class PrinterComponent : Component
{
    [ViewVariables]
    public bool IsStuck = false;

    [ViewVariables]
    [DataField("trayId", required: true)]
    public string TrayId;

    [ViewVariables]
    public int PaperQuantity = 0;

    [ViewVariables]
    public bool IsPrinting = false;
    
    [ViewVariables]
    public PrinterVisualState VisualState = PrinterVisualState.Empty;

    public float PrintingTimeLeft = 0f;

    public string FileText;
    public string FileName;
    public int FilesRemaining = 0;
}