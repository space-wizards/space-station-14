using Robust.Client.UserInterface;

namespace Content.Client.Arcade.BlockGame;

public sealed class BlockGameArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BlockGameArcadeMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BlockGameArcadeMenu>();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {

    }
}
