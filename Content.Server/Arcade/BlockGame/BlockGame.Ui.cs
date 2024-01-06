using Content.Shared.Arcade;
using System.Linq;
using Robust.Shared.Player;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    /// <summary>
    /// How often to check the currently pressed inputs for whether to move the active piece horizontally.
    /// </summary>
    private const float PressCheckSpeed = 0.08f;

    /// <summary>
    /// Whether the left button is pressed.
    /// Moves the active piece left if true.
    /// </summary>
    private bool _leftPressed = false;

    /// <summary>
    /// How long the left button has been pressed.
    /// </summary>
    private float _accumulatedLeftPressTime = 0f;

    /// <summary>
    /// Whether the right button is pressed.
    /// Moves the active piece right if true.
    /// </summary>
    private bool _rightPressed = false;

    /// <summary>
    /// How long the right button has been pressed.
    /// </summary>
    private float _accumulatedRightPressTime = 0f;

    /// <summary>
    /// Whether the down button is pressed.
    /// Speeds up how quickly the active piece falls if true.
    /// </summary>
    private bool _softDropPressed = false;


    /// <summary>
    /// Handles user input.
    /// </summary>
    /// <param name="action">The action to current player has prompted.</param>
    public void ProcessInput(BlockGamePlayerAction action)
    {
        if (_running)
        {
            switch (action)
            {
                case BlockGamePlayerAction.StartLeft:
                    _leftPressed = true;
                    break;
                case BlockGamePlayerAction.StartRight:
                    _rightPressed = true;
                    break;
                case BlockGamePlayerAction.Rotate:
                    TrySetRotation(Next(_currentRotation, false));
                    break;
                case BlockGamePlayerAction.CounterRotate:
                    TrySetRotation(Next(_currentRotation, true));
                    break;
                case BlockGamePlayerAction.SoftdropStart:
                    _softDropPressed = true;
                    if (_accumulatedFieldFrameTime > Speed)
                        _accumulatedFieldFrameTime = Speed; //to prevent jumps
                    break;
                case BlockGamePlayerAction.Harddrop:
                    PerformHarddrop();
                    break;
                case BlockGamePlayerAction.Hold:
                    HoldPiece();
                    break;
            }
        }

        switch (action)
        {
            case BlockGamePlayerAction.EndLeft:
                _leftPressed = false;
                break;
            case BlockGamePlayerAction.EndRight:
                _rightPressed = false;
                break;
            case BlockGamePlayerAction.SoftdropEnd:
                _softDropPressed = false;
                break;
            case BlockGamePlayerAction.Pause:
                _running = false;
                SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause, Started));
                break;
            case BlockGamePlayerAction.Unpause:
                if (!_gameOver && Started)
                {
                    _running = true;
                    SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));
                }
                break;
            case BlockGamePlayerAction.ShowHighscores:
                _running = false;
                SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Highscores, Started));
                break;
        }
    }

    /// <summary>
    /// Handle moving the active game piece in response to user input.
    /// </summary>
    /// <param name="frameTime">The amount of time the current game tick covers.</param>
    private void InputTick(float frameTime)
    {
        var anythingChanged = false;
        if (_leftPressed)
        {
            _accumulatedLeftPressTime += frameTime;

            while (_accumulatedLeftPressTime >= PressCheckSpeed)
            {

                if (CurrentPiece.Positions(_currentPiecePosition.AddToX(-1), _currentRotation)
                    .All(MoveCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToX(-1);
                    anythingChanged = true;
                }

                _accumulatedLeftPressTime -= PressCheckSpeed;
            }
        }

        if (_rightPressed)
        {
            _accumulatedRightPressTime += frameTime;

            while (_accumulatedRightPressTime >= PressCheckSpeed)
            {
                if (CurrentPiece.Positions(_currentPiecePosition.AddToX(1), _currentRotation)
                    .All(MoveCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToX(1);
                    anythingChanged = true;
                }

                _accumulatedRightPressTime -= PressCheckSpeed;
            }
        }

        if (anythingChanged)
            UpdateFieldUI();
    }

    /// <summary>
    /// Handles sending a message to all players/spectators.
    /// </summary>
    /// <param name="message">The message to broadcase to all players/spectators.</param>
    private void SendMessage(BoundUserInterfaceMessage message)
    {
        if (_uiSystem.TryGetUi(_owner, BlockGameUiKey.Key, out var bui))
            _uiSystem.SendUiMessage(bui, message);
    }

    /// <summary>
    /// Handles sending a message to a specific player/spectator.
    /// </summary>
    /// <param name="message">The message to send to a specific player/spectator.</param>
    /// <param name="session">The target recipient.</param>
    private void SendMessage(BoundUserInterfaceMessage message, ICommonSession session)
    {
        if (_uiSystem.TryGetUi(_owner, BlockGameUiKey.Key, out var bui))
            _uiSystem.TrySendUiMessage(bui, message, session);
    }

    /// <summary>
    /// Handles sending the current state of the game to a player that has just opened the UI.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    public void UpdateNewPlayerUI(ICommonSession session)
    {
        if (_gameOver)
        {
            SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement), session);
            return;
        }

        if (Paused)
            SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause, Started), session);
        else
            SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game, Started), session);

        FullUpdate(session);
    }

    /// <summary>
    /// Handles broadcasting the full player-visible game state to everyone who can see the game.
    /// </summary>
    private void FullUpdate()
    {
        UpdateFieldUI();
        SendHoldPieceUpdate();
        SendNextPieceUpdate();
        SendLevelUpdate();
        SendPointsUpdate();
        SendHighscoreUpdate();
    }

    /// <summary>
    /// Handles broadcasting the full player-visible game state to a specific player/spectator.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void FullUpdate(ICommonSession session)
    {
        UpdateFieldUI(session);
        SendNextPieceUpdate(session);
        SendHoldPieceUpdate(session);
        SendLevelUpdate(session);
        SendPointsUpdate(session);
        SendHighscoreUpdate(session);
    }

    /// <summary>
    /// Handles broadcasting the current location of all of the blocks in the playfield + the active piece to all spectators.
    /// </summary>
    public void UpdateFieldUI()
    {
        if (!Started)
            return;

        var computedField = ComputeField();
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField));
    }

    /// <summary>
    /// Handles broadcasting the current location of all of the blocks in the playfield + the active piece to a specific player/spectator.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    public void UpdateFieldUI(ICommonSession session)
    {
        if (!Started)
            return;

        var computedField = ComputeField();
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField), session);
    }

    /// <summary>
    /// Generates the set of blocks to send to viewers.
    /// </summary>
    public List<BlockGameBlock> ComputeField()
    {
        var result = new List<BlockGameBlock>();
        result.AddRange(_field);
        result.AddRange(CurrentPiece.Blocks(_currentPiecePosition, _currentRotation));

        var dropGhostPosition = _currentPiecePosition;
        while (CurrentPiece.Positions(dropGhostPosition.AddToY(1), _currentRotation)
                .All(DropCheck))
        {
            dropGhostPosition = dropGhostPosition.AddToY(1);
        }

        if (dropGhostPosition != _currentPiecePosition)
        {
            var blox = CurrentPiece.Blocks(dropGhostPosition, _currentRotation);
            for (var i = 0; i < blox.Length; i++)
            {
                result.Add(new BlockGameBlock(blox[i].Position, BlockGameBlock.ToGhostBlockColor(blox[i].GameBlockColor)));
            }
        }
        return result;
    }

    /// <summary>
    /// Broadcasts the state of the next queued piece to all viewers.
    /// </summary>
    private void SendNextPieceUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(NextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
    }

    /// <summary>
    /// Broadcasts the state of the next queued piece to a specific viewer.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void SendNextPieceUpdate(ICommonSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(NextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock), session);
    }

    /// <summary>
    /// Broadcasts the state of the currently held piece to all viewers.
    /// </summary>
    private void SendHoldPieceUpdate()
    {
        if (HeldPiece.HasValue)
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(HeldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock));
        else
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(Array.Empty<BlockGameBlock>(), BlockGameMessages.BlockGameVisualType.HoldBlock));
    }

    /// <summary>
    /// Broadcasts the state of the currently held piece to a specific viewer.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void SendHoldPieceUpdate(ICommonSession session)
    {
        if (HeldPiece.HasValue)
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(HeldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
        else
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(Array.Empty<BlockGameBlock>(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
    }

    /// <summary>
    /// Broadcasts the current game level to all viewers.
    /// </summary>
    private void SendLevelUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
    }

    /// <summary>
    /// Broadcasts the current game level to a specific viewer.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void SendLevelUpdate(ICommonSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level), session);
    }

    /// <summary>
    /// Broadcasts the current game score to all viewers.
    /// </summary>
    private void SendPointsUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
    }

    /// <summary>
    /// Broadcasts the current game score to a specific viewer.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void SendPointsUpdate(ICommonSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points), session);
    }

    /// <summary>
    /// Broadcasts the current game high score positions to all viewers.
    /// </summary>
    private void SendHighscoreUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(_arcadeSystem.GetLocalHighscores(), _arcadeSystem.GetGlobalHighscores()));
    }

    /// <summary>
    /// Broadcasts the current game high score positions to a specific viewer.
    /// </summary>
    /// <param name="session">The target recipient.</param>
    private void SendHighscoreUpdate(ICommonSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(_arcadeSystem.GetLocalHighscores(), _arcadeSystem.GetGlobalHighscores()), session);
    }
}
