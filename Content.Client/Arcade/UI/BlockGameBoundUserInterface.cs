using Content.Shared.Arcade.BlockGame;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

public sealed class BlockGameBoundUserInterface : BoundUserInterface
{
    private BlockGameMenu? _menu;

    public BlockGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BlockGameMenu>();
        _menu.OnAction += SendAction;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case BlockGameVisualUpdateMessage updateMessage:
                switch (updateMessage.GameVisualType)
                {
                    case BlockGameVisualType.GameField:
                        _menu?.UpdateBlocks(updateMessage.Blocks);
                        break;
                    case BlockGameVisualType.HoldBlock:
                        _menu?.UpdateHeldBlock(updateMessage.Blocks);
                        break;
                    case BlockGameVisualType.NextBlock:
                        _menu?.UpdateNextBlock(updateMessage.Blocks);
                        break;
                }
                break;
            case BlockGameScoreUpdateMessage scoreUpdate:
                _menu?.UpdatePoints(scoreUpdate.Points);
                break;
            case BlockGameUserStatusMessage userMessage:
                _menu?.SetUsability(userMessage.IsPlayer);
                break;
            case BlockGameSetScreenMessage statusMessage:
                if (statusMessage.IsStarted) _menu?.SetStarted();
                _menu?.SetScreen(statusMessage.Screen);
                if (statusMessage is BlockGameGameOverScreenMessage gameOverScreenMessage)
                    _menu?.SetGameoverInfo(gameOverScreenMessage.FinalScore, gameOverScreenMessage.LocalPlacement, gameOverScreenMessage.GlobalPlacement);
                break;
            case BlockGameHighScoreUpdateMessage highScoreUpdateMessage:
                _menu?.UpdateHighscores(highScoreUpdateMessage.LocalHighscores,
                    highScoreUpdateMessage.GlobalHighscores);
                break;
            case BlockGameLevelUpdateMessage levelUpdateMessage:
                _menu?.UpdateLevel(levelUpdateMessage.Level);
                break;
        }
    }

    public void SendAction(BlockGamePlayerAction action)
    {
        SendMessage(new BlockGamePlayerActionMessage(action));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
