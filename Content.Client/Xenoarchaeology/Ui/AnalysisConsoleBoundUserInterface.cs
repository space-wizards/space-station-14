using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class AnalysisConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AnalysisConsoleMenu? _consoleMenu;

    public AnalysisConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _consoleMenu = new AnalysisConsoleMenu();

        _consoleMenu.OnClose += Close;
        _consoleMenu.OpenCentered();

        _consoleMenu.OnServerSelectionButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleServerSelectionMessage());
        };
        _consoleMenu.OnScanButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleScanButtonPressedMessage());
        };
        _consoleMenu.OnPrintButtonPressed += () =>
        {
            SendMessage(new AnalysisConsolePrintButtonPressedMessage());
        };
        _consoleMenu.OnExtractButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleExtractButtonPressedMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case AnalysisConsoleScanUpdateState msg:
                _consoleMenu?.SetButtonsDisabled(msg);
                _consoleMenu?.UpdateInformationDisplay(msg);
                _consoleMenu?.UpdateProgressBar(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _consoleMenu?.Dispose();
    }
}

