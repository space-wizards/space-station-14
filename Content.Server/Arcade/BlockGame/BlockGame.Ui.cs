using Content.Shared.Arcade;
using Robust.Server.Player;
using System.Linq;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    private const float PressCheckSpeed = 0.08f;
    private bool _leftPressed = false;
    private float _accumulatedLeftPressTime = 0f;
    private bool _rightPressed = false;
    private float _accumulatedRightPressTime = 0f;
    private bool _softDropPressed = false;

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

    private void SendMessage(BoundUserInterfaceMessage message)
    {
        if (_uiSystem.TryGetUi(_owner, BlockGameUiKey.Key, out var bui))
            _uiSystem.SendUiMessage(bui, message);
    }

    private void SendMessage(BoundUserInterfaceMessage message, IPlayerSession session)
    {
        if (_uiSystem.TryGetUi(_owner, BlockGameUiKey.Key, out var bui))
            _uiSystem.TrySendUiMessage(bui, message, session);
    }

    public void UpdateNewPlayerUI(IPlayerSession session)
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

    private void FullUpdate()
    {
        UpdateFieldUI();
        SendHoldPieceUpdate();
        SendNextPieceUpdate();
        SendLevelUpdate();
        SendPointsUpdate();
        SendHighscoreUpdate();
    }

    private void FullUpdate(IPlayerSession session)
    {
        UpdateFieldUI(session);
        SendNextPieceUpdate(session);
        SendHoldPieceUpdate(session);
        SendLevelUpdate(session);
        SendPointsUpdate(session);
        SendHighscoreUpdate(session);
    }

    public void UpdateFieldUI()
    {
        if (!Started)
            return;

        var computedField = ComputeField();
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField));
    }

    public void UpdateFieldUI(IPlayerSession session)
    {
        if (!Started)
            return;

        var computedField = ComputeField();
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField), session);
    }

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

    private void SendNextPieceUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(NextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
    }

    private void SendNextPieceUpdate(IPlayerSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(NextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock), session);
    }

    private void SendHoldPieceUpdate()
    {
        if (HeldPiece.HasValue)
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(HeldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock));
        else
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(Array.Empty<BlockGameBlock>(), BlockGameMessages.BlockGameVisualType.HoldBlock));
    }

    private void SendHoldPieceUpdate(IPlayerSession session)
    {
        if (HeldPiece.HasValue)
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(HeldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
        else
            SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(Array.Empty<BlockGameBlock>(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
    }

    private void SendLevelUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
    }

    private void SendLevelUpdate(IPlayerSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level), session);
    }

    private void SendPointsUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
    }

    private void SendPointsUpdate(IPlayerSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points), session);
    }

    private void SendHighscoreUpdate()
    {
        SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(_arcadeSystem.GetLocalHighscores(), _arcadeSystem.GetGlobalHighscores()));
    }

    private void SendHighscoreUpdate(IPlayerSession session)
    {
        SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(_arcadeSystem.GetLocalHighscores(), _arcadeSystem.GetGlobalHighscores()), session);
    }
}
