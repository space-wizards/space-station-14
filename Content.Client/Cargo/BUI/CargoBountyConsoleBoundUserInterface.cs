using Content.Client.Cargo.UI;
using Content.Shared.Cargo.Components;
using JetBrains.Annotations;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class CargoBountyConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CargoBountyMenu? _menu;

    public CargoBountyConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnClose += Close;

        _menu.OnLabelButtonPressed += id =>
        {
            SendMessage(new BountyPrintLabelMessage(id));
        };

        _menu.OnSkipButtonPressed += id =>
        {
            SendMessage(new BountySkipMessage(id));
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not CargoBountyConsoleState state)
            return;

        _menu?.UpdateEntries(state.Bounties, state.UntilNextSkip);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
