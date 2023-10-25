using Content.Shared.BluespaceHarvester;
using JetBrains.Annotations;

namespace Content.Client.BluespaceHarvester;

[UsedImplicitly]
public sealed class BluespaceHarvesterBoundUserInterface : BoundUserInterface
{
    private BluespaceHarvesterMenu? _window;

    public BluespaceHarvesterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new BluespaceHarvesterMenu(this);
        _window.OnClose += Close;
        _window?.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
        _window = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BluespaceHarvesterBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendTargetLevel(int level)
    {
        SendMessage(new BluespaceHarvesterTargetLevelMessage(level));
    }

    public void SendBuy(Shared.BluespaceHarvester.BluespaceHarvesterCategory category)
    {
        SendMessage(new BluespaceHarvesterBuyMessage(category));
    }
}
