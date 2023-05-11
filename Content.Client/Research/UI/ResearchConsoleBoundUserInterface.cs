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

        _consoleMenu.OnClose += Close;

        _consoleMenu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (ResearchConsoleBoundInterfaceState)state;
        _consoleMenu?.UpdatePanels();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _consoleMenu?.Dispose();
    }
}
