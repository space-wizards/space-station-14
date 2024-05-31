using Content.Shared.Thief;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Thief;

[UsedImplicitly]
public sealed class ThiefBackpackBoundUserInterface : BoundUserInterface
{
    private ThiefBackpackMenu? _window;

    public ThiefBackpackBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new ThiefBackpackMenu(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ThiefBackpackBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new ThiefBackpackChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new ThiefBackpackApproveMessage());
    }
}
