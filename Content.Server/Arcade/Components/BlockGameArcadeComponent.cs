using System.Linq;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server.Arcade.Components
{
    [RegisterComponent]
    public sealed class BlockGameArcadeComponent : Component
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public bool Powered => _entityManager.TryGetComponent<ApcPowerReceiverComponent>(Owner, out var powerReceiverComponent) && powerReceiverComponent.Powered;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(BlockGameUiKey.Key);

        private BlockGame? _game;

        private IPlayerSession? _player;
        private readonly List<IPlayerSession> _spectators = new();

        public void RegisterPlayerSession(IPlayerSession session)
        {
            if (_player == null) _player = session;
            else _spectators.Add(session);

            UpdatePlayerStatus(session);
            _game?.UpdateNewPlayerUI(session);
        }

        private void DeactivePlayer(IPlayerSession session)
        {
            if (_player != session) return;

            var temp = _player;
            _player = null;
            if (_spectators.Count != 0)
            {
                _player = _spectators[0];
                _spectators.Remove(_player);
                UpdatePlayerStatus(_player);
            }
            _spectators.Add(temp);

            UpdatePlayerStatus(temp);
        }

        public void UnRegisterPlayerSession(IPlayerSession session)
        {
            if (_player == session)
            {
                DeactivePlayer(_player);
            }
            else
            {
                _spectators.Remove(session);
                UpdatePlayerStatus(session);
            }
        }

        private void UpdatePlayerStatus(IPlayerSession session)
        {
            UserInterface?.SendMessage(new BlockGameMessages.BlockGameUserStatusMessage(_player == session), session);
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
            _game = new BlockGame(this);
        }

        public void OnPowerStateChanged(PowerChangedEvent e)
        {
            if (e.Powered) return;

            UserInterface?.CloseAll();
            _player = null;
            _spectators.Clear();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case BlockGameMessages.BlockGamePlayerActionMessage playerActionMessage:
                    if (obj.Session != _player) break;

                    if (playerActionMessage.PlayerAction == BlockGamePlayerAction.NewGame)
                    {
                        if(_game?.Started == true) _game = new BlockGame(this);
                        _game?.StartGame();
                    }
                    else
                    {
                        _game?.ProcessInput(playerActionMessage.PlayerAction);
                    }

                    break;
            }
        }

        public void DoGameTick(float frameTime)
        {
            _game?.GameTick(frameTime);
        }

        private sealed class BlockGame
        {
            //note: field is 10(0 -> 9) wide and 20(0 -> 19) high

            private readonly BlockGameArcadeComponent _component;

            private readonly List<BlockGameBlock> _field = new();

            private BlockGamePiece _currentPiece;

            private BlockGamePiece _nextPiece
            {
                get => _internalNextPiece;
                set
                {
                    _internalNextPiece = value;
                    SendNextPieceUpdate();
                }
            }
            private BlockGamePiece _internalNextPiece;

            private void SendNextPieceUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
            }

            private void SendNextPieceUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock), session);
            }

            private bool _holdBlock = false;
            private BlockGamePiece? _heldPiece
            {
                get => _internalHeldPiece;
                set
                {
                    _internalHeldPiece = value;
                    SendHoldPieceUpdate();
                }
            }

            private BlockGamePiece? _internalHeldPiece = null;

            private void SendHoldPieceUpdate()
            {
                if(_heldPiece.HasValue) _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_heldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock));
                else _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(new BlockGameBlock[0], BlockGameMessages.BlockGameVisualType.HoldBlock));
            }

            private void SendHoldPieceUpdate(IPlayerSession session)
            {
                if(_heldPiece.HasValue) _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_heldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
                else _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(new BlockGameBlock[0], BlockGameMessages.BlockGameVisualType.HoldBlock), session);
            }

            private Vector2i _currentPiecePosition;
            private BlockGamePieceRotation _currentRotation;
            private float _softDropModifier = 0.1f;

            private float Speed =>
                -0.03f * Level + 1 * (!_softDropPressed ? 1 : _softDropModifier);

            private const float _pressCheckSpeed = 0.08f;

            private bool _running;
            public bool Paused => !(_running && _started);
            private bool _started;
            public bool Started => _started;
            private bool _gameOver;

            private bool _leftPressed;
            private bool _rightPressed;
            private bool _softDropPressed;

            private int Points
            {
                get => _internalPoints;
                set
                {
                    if (_internalPoints == value) return;
                    _internalPoints = value;
                    SendPointsUpdate();
                }
            }
            private int _internalPoints;

            private ArcadeSystem.HighScorePlacement? _highScorePlacement = null;

            private void SendPointsUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
            }

            private void SendPointsUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
            }

            public int Level
            {
                get => _level;
                set
                {
                    _level = value;
                    SendLevelUpdate();
                }
            }
            private int _level = 0;
            private void SendLevelUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
            }

            private void SendLevelUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
            }

            private int ClearedLines
            {
                get => _clearedLines;
                set
                {
                    _clearedLines = value;

                    if (_clearedLines < LevelRequirement) return;

                    _clearedLines -= LevelRequirement;
                    Level++;
                }
            }

            private int _clearedLines = 0;
            private int LevelRequirement => Math.Min(100, Math.Max(Level * 10 - 50, 10));

            public BlockGame(BlockGameArcadeComponent component)
            {
                _component = component;
                _allBlockGamePieces = (BlockGamePieceType[]) Enum.GetValues(typeof(BlockGamePieceType));
                _internalNextPiece = GetRandomBlockGamePiece(_component._random);
                InitializeNewBlock();
            }

            private void SendHighscoreUpdate()
            {
                var entitySystem = EntitySystem.Get<ArcadeSystem>();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(entitySystem.GetLocalHighscores(), entitySystem.GetGlobalHighscores()));
            }

            private void SendHighscoreUpdate(IPlayerSession session)
            {
                var entitySystem = EntitySystem.Get<ArcadeSystem>();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(entitySystem.GetLocalHighscores(), entitySystem.GetGlobalHighscores()), session);
            }

            public void StartGame()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));

                FullUpdate();

                _running = true;
                _started = true;
            }

            private void FullUpdate()
            {
                UpdateAllFieldUI();
                SendHoldPieceUpdate();
                SendNextPieceUpdate();
                SendPointsUpdate();
                SendHighscoreUpdate();
                SendLevelUpdate();
            }

            private void FullUpdate(IPlayerSession session)
            {
                UpdateFieldUI(session);
                SendPointsUpdate(session);
                SendNextPieceUpdate(session);
                SendHoldPieceUpdate(session);
                SendHighscoreUpdate(session);
                SendLevelUpdate(session);
            }

            public void GameTick(float frameTime)
            {
                if (!_running) return;

                InputTick(frameTime);

                FieldTick(frameTime);
            }

            private float _accumulatedLeftPressTime;
            private float _accumulatedRightPressTime;
            private void InputTick(float frameTime)
            {
                bool anythingChanged = false;
                if (_leftPressed)
                {
                    _accumulatedLeftPressTime += frameTime;

                    while (_accumulatedLeftPressTime >= _pressCheckSpeed)
                    {

                        if (_currentPiece.Positions(_currentPiecePosition.AddToX(-1), _currentRotation)
                            .All(MoveCheck))
                        {
                            _currentPiecePosition = _currentPiecePosition.AddToX(-1);
                            anythingChanged = true;
                        }

                        _accumulatedLeftPressTime -= _pressCheckSpeed;
                    }
                }

                if (_rightPressed)
                {
                    _accumulatedRightPressTime += frameTime;

                    while (_accumulatedRightPressTime >= _pressCheckSpeed)
                    {
                        if (_currentPiece.Positions(_currentPiecePosition.AddToX(1), _currentRotation)
                            .All(MoveCheck))
                        {
                            _currentPiecePosition = _currentPiecePosition.AddToX(1);
                            anythingChanged = true;
                        }

                        _accumulatedRightPressTime -= _pressCheckSpeed;
                    }
                }

                if(anythingChanged) UpdateAllFieldUI();
            }

            private float _accumulatedFieldFrameTime;
            private void FieldTick(float frameTime)
            {
                _accumulatedFieldFrameTime += frameTime;

                // Speed goes negative sometimes. uhhhh max() it I guess!!!
                var checkTime = Math.Max(0.03f, Speed);

                while (_accumulatedFieldFrameTime >= checkTime)
                {
                    if (_softDropPressed) AddPoints(1);

                    InternalFieldTick();

                    _accumulatedFieldFrameTime -= checkTime;
                }
            }

            private void InternalFieldTick()
            {
                if (_currentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
                    .All(DropCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToY(1);
                }
                else
                {
                    var blocks = _currentPiece.Blocks(_currentPiecePosition, _currentRotation);
                    _field.AddRange(blocks);

                    //check loose conditions
                    if (IsGameOver)
                    {
                        InvokeGameover();
                        return;
                    }

                    InitializeNewBlock();
                }

                CheckField();

                UpdateAllFieldUI();
            }

            private void CheckField()
            {
                int pointsToAdd = 0;
                int consecutiveLines = 0;
                int clearedLines = 0;
                for (int y = 0; y < 20; y++)
                {
                    if (CheckLine(y))
                    {
                        //line was cleared
                        y--;
                        consecutiveLines++;
                        clearedLines++;
                    }
                    else if(consecutiveLines != 0)
                    {
                        var mod = consecutiveLines switch
                        {
                            1 => 40,
                            2 => 100,
                            3 => 300,
                            4 => 1200,
                            _ => 0
                        };
                        pointsToAdd += mod * (_level + 1);
                    }
                }

                ClearedLines += clearedLines;
                AddPoints(pointsToAdd);
            }

            private bool CheckLine(int y)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (!_field.Any(b => b.Position.X == x && b.Position.Y == y)) return false;
                }

                //clear line
                _field.RemoveAll(b => b.Position.Y == y);
                //move everything down
                FillLine(y);

                return true;
            }

            private void AddPoints(int amount)
            {
                if (amount == 0) return;

                Points += amount;
            }

            private void FillLine(int y)
            {
                for (int c_y = y; c_y > 0; c_y--)
                {
                    for (int j = 0; j < _field.Count; j++)
                    {
                        if(_field[j].Position.Y != c_y-1) continue;

                        _field[j] = new BlockGameBlock(_field[j].Position.AddToY(1), _field[j].GameBlockColor);
                    }
                }
            }

            private void InitializeNewBlock()
            {
                InitializeNewBlock(_nextPiece);
                _nextPiece = GetRandomBlockGamePiece(_component._random);
                _holdBlock = false;

                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
            }

            private void InitializeNewBlock(BlockGamePiece piece)
            {
                _currentPiecePosition = new Vector2i(5,0);

                _currentRotation = BlockGamePieceRotation.North;

                _currentPiece = piece;
                UpdateAllFieldUI();
            }

            private bool LowerBoundCheck(Vector2i position) => position.Y < 20;
            private bool BorderCheck(Vector2i position) => position.X >= 0 && position.X < 10;
            private bool ClearCheck(Vector2i position) => _field.All(block => !position.Equals(block.Position));

            private bool DropCheck(Vector2i position) => LowerBoundCheck(position) && ClearCheck(position);
            private bool MoveCheck(Vector2i position) => BorderCheck(position) && ClearCheck(position);
            private bool RotateCheck(Vector2i position) => BorderCheck(position) && LowerBoundCheck(position) && ClearCheck(position);

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
                            if (_accumulatedFieldFrameTime > Speed) _accumulatedFieldFrameTime = Speed; //to prevent jumps
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
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause, _started));
                        break;
                    case BlockGamePlayerAction.Unpause:
                        if (!_gameOver && _started)
                        {
                            _running = true;
                            _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));
                        }
                        break;
                    case BlockGamePlayerAction.ShowHighscores:
                        _running = false;
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Highscores, _started));
                        break;
                }
            }

            private void TrySetRotation(BlockGamePieceRotation rotation)
            {
                if(!_running) return;

                if (!_currentPiece.CanSpin) return;

                if (!_currentPiece.Positions(_currentPiecePosition, rotation)
                    .All(RotateCheck))
                {
                    return;
                }

                _currentRotation = rotation;
                UpdateAllFieldUI();
            }

            private void HoldPiece()
            {
                if (!_running) return;

                if (_holdBlock) return;

                var tempHeld = _heldPiece;
                _heldPiece = _currentPiece;
                _holdBlock = true;

                if (!tempHeld.HasValue)
                {
                    InitializeNewBlock();
                    return;
                }

                InitializeNewBlock(tempHeld.Value);
            }

            private void PerformHarddrop()
            {
                int spacesDropped = 0;
                while (_currentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
                    .All(DropCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToY(1);
                    spacesDropped++;
                }
                AddPoints(spacesDropped * 2);

                InternalFieldTick();
            }

            public void UpdateAllFieldUI()
            {
                if (!_started) return;

                var computedField = ComputeField();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField));
            }

            public void UpdateFieldUI(IPlayerSession session)
            {
                if (!_started) return;

                var computedField = ComputeField();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField), session);
            }

            private bool IsGameOver => _field.Any(block => block.Position.Y == 0);

            private void InvokeGameover()
            {
                _running = false;
                _gameOver = true;

                if (_component._player?.AttachedEntity is {Valid: true} playerEntity)
                {
                    var blockGameSystem = EntitySystem.Get<ArcadeSystem>();

                    _highScorePlacement = blockGameSystem.RegisterHighScore(IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(playerEntity).EntityName, Points);
                    SendHighscoreUpdate();
                }
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement));
            }

            public void UpdateNewPlayerUI(IPlayerSession session)
            {
                if (_gameOver)
                {
                    _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement), session);
                }
                else
                {
                    if (Paused)
                    {
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause, Started), session);
                    }
                    else
                    {
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game, Started), session);
                    }
                }

                FullUpdate(session);
            }

            public List<BlockGameBlock> ComputeField()
            {
                var result = new List<BlockGameBlock>();
                result.AddRange(_field);
                result.AddRange(_currentPiece.Blocks(_currentPiecePosition, _currentRotation));

                var dropGhostPosition = _currentPiecePosition;
                while (_currentPiece.Positions(dropGhostPosition.AddToY(1), _currentRotation)
                       .All(DropCheck))
                {
                    dropGhostPosition = dropGhostPosition.AddToY(1);
                }

                if (dropGhostPosition != _currentPiecePosition)
                {
                    var blox = _currentPiece.Blocks(dropGhostPosition, _currentRotation);
                    for (var i = 0; i < blox.Length; i++)
                    {
                        result.Add(new BlockGameBlock(blox[i].Position, BlockGameBlock.ToGhostBlockColor(blox[i].GameBlockColor)));
                    }
                }
                return result;
            }

            private enum BlockGamePieceType
            {
                I,
                L,
                LInverted,
                S,
                SInverted,
                T,
                O
            }

            private enum BlockGamePieceRotation
            {
                North,
                East,
                South,
                West
            }

            private static BlockGamePieceRotation Next(BlockGamePieceRotation rotation, bool inverted)
            {
                return rotation switch
                {
                    BlockGamePieceRotation.North => inverted ? BlockGamePieceRotation.West : BlockGamePieceRotation.East,
                    BlockGamePieceRotation.East => inverted ? BlockGamePieceRotation.North : BlockGamePieceRotation.South,
                    BlockGamePieceRotation.South => inverted ? BlockGamePieceRotation.East : BlockGamePieceRotation.West,
                    BlockGamePieceRotation.West => inverted ? BlockGamePieceRotation.South : BlockGamePieceRotation.North,
                    _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
                };
            }

            private readonly BlockGamePieceType[] _allBlockGamePieces;

            private List<BlockGamePieceType> _blockGamePiecesBuffer = new();

            private BlockGamePiece GetRandomBlockGamePiece(IRobustRandom random)
            {
                if (_blockGamePiecesBuffer.Count == 0)
                {
                    _blockGamePiecesBuffer = _allBlockGamePieces.ToList();
                }

                var chosenPiece = random.Pick(_blockGamePiecesBuffer);
                _blockGamePiecesBuffer.Remove(chosenPiece);
                return BlockGamePiece.GetPiece(chosenPiece);
            }

            private struct BlockGamePiece
            {
                public Vector2i[] Offsets;
                private BlockGameBlock.BlockGameBlockColor _gameBlockColor;
                public bool CanSpin;

                public Vector2i[] Positions(Vector2i center,
                    BlockGamePieceRotation rotation)
                {
                    return RotatedOffsets(rotation).Select(v => center + v).ToArray();
                }

                private Vector2i[] RotatedOffsets(BlockGamePieceRotation rotation)
                {
                    Vector2i[] rotatedOffsets = (Vector2i[])Offsets.Clone();
                    //until i find a better algo
                    var amount = rotation switch
                    {
                        BlockGamePieceRotation.North => 0,
                        BlockGamePieceRotation.East => 1,
                        BlockGamePieceRotation.South => 2,
                        BlockGamePieceRotation.West => 3,
                        _ => 0
                    };

                    for (var i = 0; i < amount; i++)
                    {
                        for (var j = 0; j < rotatedOffsets.Length; j++)
                        {
                            rotatedOffsets[j] = rotatedOffsets[j].Rotate90DegreesAsOffset();
                        }
                    }

                    return rotatedOffsets;
                }

                public BlockGameBlock[] Blocks(Vector2i center,
                    BlockGamePieceRotation rotation)
                {
                    var positions = Positions(center, rotation);
                    var result = new BlockGameBlock[positions.Length];
                    var i = 0;
                    foreach (var position in positions)
                    {
                        result[i++] = position.ToBlockGameBlock(_gameBlockColor);
                    }

                    return result;
                }

                public BlockGameBlock[] BlocksForPreview()
                {
                    var xOffset = 0;
                    var yOffset = 0;
                    foreach (var offset in Offsets)
                    {
                        if (offset.X < xOffset) xOffset = offset.X;
                        if (offset.Y < yOffset) yOffset = offset.Y;
                    }

                    return Blocks(new Vector2i(-xOffset, -yOffset), BlockGamePieceRotation.North);
                }

                public static BlockGamePiece GetPiece(BlockGamePieceType type)
                {
                    //switch statement, hardcoded offsets
                    return type switch
                    {
                        BlockGamePieceType.I => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(0, 2),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.LightBlue,
                            CanSpin = true
                        },
                        BlockGamePieceType.L => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(1, 1),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Orange,
                            CanSpin = true
                        },
                        BlockGamePieceType.LInverted => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(-1, 1),
                                new Vector2i(0, 1),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Blue,
                            CanSpin = true
                        },
                        BlockGamePieceType.S => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(-1, 0),
                                new Vector2i(0, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Green,
                            CanSpin = true
                        },
                        BlockGamePieceType.SInverted => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(-1, -1), new Vector2i(0, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Red,
                            CanSpin = true
                        },
                        BlockGamePieceType.T => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1),
                                new Vector2i(-1, 0), new Vector2i(0, 0), new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Purple,
                            CanSpin = true
                        },
                        BlockGamePieceType.O => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Yellow,
                            CanSpin = false
                        },
                        _ => new BlockGamePiece {Offsets = new[] {new Vector2i(0, 0)}}
                    };
                }
            }
        }
    }
}
