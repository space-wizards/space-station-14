#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.Arcade;
using Content.Shared.GameObjects;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Arcade
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class TetrisArcadeComponent : Component, IActivate
    {
        public override string Name => "TetrisArcade";
        public override uint? NetID => ContentNetIDs.TETRIS_ARCADE;
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(TetrisArcadeUiKey.Key);

        private TetrisGame? _game;

        public void Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            if (!Powered)
            {
                return;
            }

            UserInterface?.Toggle(actor.playerSession);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (!(obj.Message is TetrisMessages.TetrisPlayerActionMessage message)) return;
            if (message.PlayerAction == TetrisPlayerAction.NewGame)
            {
                _game = new TetrisGame(this);
            }
            else
            {
                _game?.ProcessInput(message.PlayerAction);
            }
        }

        public void DoGameTick(float frameTime)
        {
            _game?.GameTick(frameTime);
        }

        private class TetrisGame : IDisposable
        {
            //note: field is 10(0 -> 9) wide and 20(0 -> 19) high

            private TetrisArcadeComponent _component;

            private List<TetrisBlock> _field = new List<TetrisBlock>();

            private TetrisPiece _currentPiece;
            private Vector2i _currentPiecePosition;
            private TetrisPieceRotation _currentRotation;
            private float _softDropOverride = 0.1f;
            private float _speed = 0.5f;

            private float _pressCheckSpeed = 0.1f;

            private bool _running;
            private bool _initialized;
            private bool _gameOver;

            private bool _leftPressed;
            private bool _rightPressed;
            private bool _softDropPressed;

            public TetrisGame(TetrisArcadeComponent component)
            {
                _component = component;
            }

            public void StartGame()
            {
                InitializeNewBlock();

                UpdateUI();

                _running = true;
                _initialized = true;
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

                    if (_accumulatedLeftPressTime >= _pressCheckSpeed)
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

                    if (_accumulatedRightPressTime >= _pressCheckSpeed)
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

                if(anythingChanged) UpdateUI();
            }

            private float _accumulatedFieldFrameTime;
            private void FieldTick(float frameTime)
            {
                _accumulatedFieldFrameTime += frameTime;

                var checkTime = _softDropPressed && _speed > _softDropOverride ? _softDropOverride : _speed;

                if (_accumulatedFieldFrameTime < checkTime) return;

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

                UpdateUI();

                _accumulatedFieldFrameTime -= checkTime;
            }

            private void InitializeNewBlock()
            {
                _currentPiecePosition = new Vector2i(5,0);

                _currentRotation = TetrisPieceRotation.North;

                _currentPiece = TetrisPiece.GetRandom();
            }

            private bool DropCheck(Vector2i position) => position.Y < 20 && _field.All(block => !position.Equals(block.Position));

            private bool MoveCheck(Vector2i position) => position.X >= 0 && position.X < 10 &&
                                                         _field.All(block => !position.Equals(block.Position));

            private bool IsGameOver => _field.Any(block => block.Position.Y == 0);

            private void InvokeGameover()
            {
                _running = false;
                _gameOver = true;
                //todo add feedback
            }

            public void ProcessInput(TetrisPlayerAction action)
            {
                switch (action)
                {
                    case TetrisPlayerAction.StartLeft:
                        _leftPressed = true;
                        break;
                    case TetrisPlayerAction.EndLeft:
                        _leftPressed = false;
                        break;
                    case TetrisPlayerAction.StartRight:
                        _rightPressed = true;
                        break;
                    case TetrisPlayerAction.EndRight:
                        _rightPressed = false;
                        break;
                    case TetrisPlayerAction.Rotate:
                        _currentRotation = Next(_currentRotation, false);
                        UpdateUI();
                        break;
                    case TetrisPlayerAction.CounterRotate:
                        _currentRotation = Next(_currentRotation, true);
                        UpdateUI();
                        break;
                    case TetrisPlayerAction.SoftdropStart:
                        _softDropPressed = true;
                        break;
                    case TetrisPlayerAction.SoftdropEnd:
                        _softDropPressed = false;
                        break;
                    case TetrisPlayerAction.Harddrop:
                        PerformHarddrop();
                        break;
                    case TetrisPlayerAction.StartGame:
                        StartGame();
                        break;
                    case TetrisPlayerAction.Pause:
                        _running = false;
                        break;
                    case TetrisPlayerAction.Unpause:
                        if (!_gameOver) _running = true;
                        break;
                    case TetrisPlayerAction.Hold:
                        break;
                }
            }

            private void PerformHarddrop()
            {
                //todo move piece to lowest possible position (and force a gametick)
            }

            public void UpdateUI()
            {
                if (!_initialized) return;

                var computedField = ComputeField();
                _component.UserInterface?.SendMessage(new TetrisMessages.TetrisUIUpdateMessage(computedField.ToArray()));
            }

            public List<TetrisBlock> ComputeField()
            {
                var result = new List<TetrisBlock>();
                result.AddRange(_field);
                result.AddRange(_currentPiece.Blocks(_currentPiecePosition, _currentRotation));
                return result;
            }

            private enum TetrisPieceType
            {
                I,
                L,
                LInverted,
                S,
                SInverted,
                T,
                Block
            }

            private enum TetrisPieceRotation
            {
                North,
                East,
                South,
                West
            }

            private static TetrisPieceRotation Next(TetrisPieceRotation rotation, bool inverted)
            {
                return rotation switch
                {
                    TetrisPieceRotation.North => inverted ? TetrisPieceRotation.West : TetrisPieceRotation.East,
                    TetrisPieceRotation.East => inverted ? TetrisPieceRotation.North : TetrisPieceRotation.South,
                    TetrisPieceRotation.South => inverted ? TetrisPieceRotation.East : TetrisPieceRotation.West,
                    TetrisPieceRotation.West => inverted ? TetrisPieceRotation.South : TetrisPieceRotation.North,
                    _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
                };
            }

            private struct TetrisPiece
            {
                private Vector2i[] _offsets;
                private TetrisBlock.TetrisBlockColor _color;

                public Vector2i[] Positions(Vector2i center,
                    TetrisPieceRotation rotation)
                {
                    return RotatedOffsets(rotation).Select(v => center + v).ToArray();
                }

                private Vector2i[] RotatedOffsets(TetrisPieceRotation rotation)
                {
                    Vector2i[] rotatedOffsets = (Vector2i[])_offsets.Clone();
                    //until i find a better algo
                    var amount = rotation switch
                    {
                        TetrisPieceRotation.North => 0,
                        TetrisPieceRotation.East => 1,
                        TetrisPieceRotation.South => 2,
                        TetrisPieceRotation.West => 3,
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

                public TetrisBlock[] Blocks(Vector2i center,
                    TetrisPieceRotation rotation)
                {
                    var positions = Positions(center, rotation);
                    var result = new TetrisBlock[positions.Length];
                    var i = 0;
                    foreach (var position in positions)
                    {
                        result[i++] = position.ToTetrisBlock(_color);
                    }

                    return result;
                }

                public static TetrisPiece GetRandom()
                {
                    var random = IoCManager.Resolve<IRobustRandom>();
                    var pieces = (TetrisPieceType[])Enum.GetValues(typeof(TetrisPieceType));
                    var choice = random.Pick(pieces);
                    return GetPiece(choice);
                }

                public static TetrisPiece GetPiece(TetrisPieceType type)
                {
                    //switch statement, hardcoded offsets
                    return type switch
                    {
                        TetrisPieceType.I => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(0, 2),
                            },
                            _color = TetrisBlock.TetrisBlockColor.LightBlue
                        },
                        TetrisPieceType.L => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(1, 1),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Orange
                        },
                        TetrisPieceType.LInverted => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(-1, 1),
                                new Vector2i(0, 1),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Blue
                        },
                        TetrisPieceType.S => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(-1, 0),
                                new Vector2i(0, 0),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Green
                        },
                        TetrisPieceType.SInverted => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(-1, -1), new Vector2i(0, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Red
                        },
                        TetrisPieceType.T => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(-1, -1), new Vector2i(0, -1), new Vector2i(1, -1),
                                new Vector2i(0, 0),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Purple
                        },
                        TetrisPieceType.Block => new TetrisPiece
                        {
                            _offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _color = TetrisBlock.TetrisBlockColor.Yellow
                        },
                        _ => new TetrisPiece {_offsets = new[] {new Vector2i(0, 0)}}
                    };
                }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
