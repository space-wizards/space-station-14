using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Research.UI;

[UsedImplicitly]
public sealed class ResearchConsoleBoundUserInterface : BoundUserInterface
{

    private ResearchConsoleMenu? _consoleMenu;


    public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        SendMessage(new ConsoleServerSyncMessage());
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner.Owner;

        _consoleMenu = new ResearchConsoleMenu(owner);

        _consoleMenu.OnTechnologyCardPressed += id =>
        {
            SendMessage(new ConsoleUnlockTechnologyMessage(id));
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };

        _consoleMenu.OnSyncButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSyncMessage());
        };

        _consoleMenu.OnClose += Close;

        _consoleMenu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
            return;
        _consoleMenu?.UpdatePanels(castState);
        _consoleMenu?.UpdateInformationPanel(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _consoleMenu?.Dispose();
    }
}
