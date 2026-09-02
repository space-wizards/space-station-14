using Content.Shared.Arcade;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

/// <summary>
/// A BUI for the block game arcade machine, wraps an <see cref="BlockGameMenu"/>.
/// </summary>
/// <seealso cref="BlockGameArcadeComponent"/>
public sealed class BlockGameBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BlockGameMenu? _menu;

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
            case BlockGameMessages.BlockGameVisualUpdateMessage updateMessage:
                switch (updateMessage.GameVisualType)
                {
                    case BlockGameMessages.BlockGameVisualType.GameField:
                        _menu?.UpdateBlocks(updateMessage.Blocks);
                        break;
                    case BlockGameMessages.BlockGameVisualType.HoldBlock:
                        _menu?.UpdateHeldBlock(updateMessage.Blocks);
                        break;
                    case BlockGameMessages.BlockGameVisualType.NextBlock:
                        _menu?.UpdateNextBlock(updateMessage.Blocks);
                        break;
                }
                break;
            case BlockGameMessages.BlockGameScoreUpdateMessage scoreUpdate:
                _menu?.UpdatePoints(scoreUpdate.Points);
                break;
            case BlockGameMessages.BlockGameUserStatusMessage userMessage:
                _menu?.SetUsability(userMessage.IsPlayer);
                break;
            case BlockGameMessages.BlockGameSetScreenMessage statusMessage:
                if (statusMessage.IsStarted) _menu?.SetStarted();
                _menu?.SetScreen(statusMessage.Screen);
                if (statusMessage is BlockGameMessages.BlockGameGameOverScreenMessage gameOverScreenMessage)
                    _menu?.SetGameoverInfo(gameOverScreenMessage.FinalScore, gameOverScreenMessage.LocalPlacement, gameOverScreenMessage.GlobalPlacement);
                break;
            case BlockGameMessages.BlockGameHighScoreUpdateMessage highScoreUpdateMessage:
                _menu?.UpdateHighscores(highScoreUpdateMessage.LocalHighscores,
                    highScoreUpdateMessage.GlobalHighscores);
                break;
            case BlockGameMessages.BlockGameLevelUpdateMessage levelUpdateMessage:
                _menu?.UpdateLevel(levelUpdateMessage.Level);
                break;
        }
    }

    public void SendAction(BlockGamePlayerAction action)
    {
        SendMessage(new BlockGameMessages.BlockGamePlayerActionMessage(action));
    }
}
