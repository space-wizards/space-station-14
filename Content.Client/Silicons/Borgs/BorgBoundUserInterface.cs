using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Borgs;

[UsedImplicitly]
public sealed class BorgBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BorgMenu? _consoleMenu;

    public BorgBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner;

        _consoleMenu = new BorgMenu(owner);



        _consoleMenu.OnClose += Close;

        _consoleMenu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _consoleMenu?.Dispose();
    }
}
