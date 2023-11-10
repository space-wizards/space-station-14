using Content.Shared.Thief;
using JetBrains.Annotations;

namespace Content.Client.Thief;

[UsedImplicitly]
public sealed class ThiefBackpackBoundUserInterface : BoundUserInterface
{
    private ThiefBackpackMenu? _window;

    public ThiefBackpackBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

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

        _window?.Dispose();
        _window = null;
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ThiefBackpackBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    //Sends

    //public void SendTargetLevel(int level)
    //{
    //    SendMessage(new BluespaceHarvesterTargetLevelMessage(level));
    //}
    //
    //public void SendBuy(Shared.BluespaceHarvester.BluespaceHarvesterCategory category)
    //{
    //    SendMessage(new BluespaceHarvesterBuyMessage(category));
    //}
}
