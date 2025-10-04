using Robust.Client.UserInterface;
using static Content.Shared.Arcade.MineGameShared;

namespace Content.Client.Arcade.UI;

public sealed class MineGameBoundUserInterface : BoundUserInterface
{
    [ViewVariables] private MineGameArcadeWindow? _menu;

    public MineGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        SendMessage(new MineGameRequestDataMessage());
    }

    public void SendTileAction(MineGameTileAction action)
    {
        SendMessage(new MineGameTileActionMessage(action));
    }

    public void SetBoardAction(MineGameBoardSettings settings)
    {
        SendMessage(new MineGameRequestNewBoardMessage(settings));
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MineGameArcadeWindow>();
        _menu.OnTileAction += SendTileAction;
        _menu.OnBoardSettingAction += SetBoardAction;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is MineGameBoardUpdateMessage msg)
            _menu?.UpdateBoard(msg.BoardWidth, msg.TileStates, msg.Metadata);
    }
}
