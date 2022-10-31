using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class AnalysisConsoleBoundUserInterface : BoundUserInterface
{
    public EntityUid AnalysisConsole;

    private AnalysisConsoleMenu? _consoleMenu;

    public AnalysisConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        AnalysisConsole = owner.Owner;
    }

    protected override void Open()
    {
        base.Open();

        _consoleMenu = new AnalysisConsoleMenu();

        _consoleMenu.OnClose += Close;
        _consoleMenu.OpenCentered();

        _consoleMenu.OnServerSelectionButtonPressed += _ =>
        {
            SendMessage(new AnalysisConsoleServerSelectionMessage());
        };
        _consoleMenu.OnScanButtonPressed += _ =>
        {
            SendMessage(new AnalysisConsoleScanButtonPressedMessage());
        };
        _consoleMenu.OnDestroyButtonPressed += _ =>
        {
            SendMessage(new AnalysisConsoleDestroyButtonPressedMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_consoleMenu == null)
            return;

        switch (state)
        {
            case AnalysisConsoleScanUpdateState msg:
                _consoleMenu.UpdateArtifactIcon(msg.Artifact);
                _consoleMenu.SetDestroyButtonDisabled(!msg.ServerConnected);
                _consoleMenu.SetScanButtonDisabled(!msg.AnalyzerConnected);
                _consoleMenu.UpdateInformationDisplay(msg);
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

