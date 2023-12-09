using JetBrains.Annotations;
using Content.Shared.Printer;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Printer.UI;

[UsedImplicitly]
public sealed class PrinterBoundUi : BoundUserInterface
{
    [Dependency] public readonly IFileDialogManager _fileDialogManager = default!;

    private PrinterWindow? _window;

    public PrinterBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();

        _window = new PrinterWindow(this);
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    public void PrintFileContents(string text, int number, string name)
    {
        if(number > 0)
            SendMessage(new PrinterPrintFile(text, number, name));
    }
}