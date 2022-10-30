using Content.Client.Research;
using Content.Client.Research.UI;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class AnalysisConsoleBoundUserInterface : BoundUserInterface
{
    private AnalysisConsoleMenu? _consoleMenu;

    public AnalysisConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        SendMessage(new ConsoleServerSyncMessage());
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
                _consoleMenu.SetScanButtonDisabled(!msg.AnalyzerConnected);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _consoleMenu?.Dispose();
    }
}

