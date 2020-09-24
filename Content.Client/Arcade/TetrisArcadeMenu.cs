using System;
using Content.Client.GameObjects.Components.Arcade;
using Content.Shared.Arcade;
using Content.Shared.Input;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Arcade
{
    public class TetrisArcadeMenu : SS14Window
    {
        private TetrisArcadeBoundUserInterface _owner;

        private GridContainer _gameGrid;
        private Button _startButton;

        public TetrisArcadeMenu(TetrisArcadeBoundUserInterface owner)
        {
            Title = "Tetris!";
            _owner = owner;

            var mainGrid = new GridContainer
            {
                Columns = 2
            };

            _gameGrid = new GridContainer
            {
                Columns = 10,
                HSeparationOverride = 0,
                VSeparationOverride = 0
            };
            UpdateBlocks(new TetrisBlock[0]);
            mainGrid.AddChild(_gameGrid);


            var infoGrid = new GridContainer
            {
                Columns = 1
            };

            var holdContainer = new PanelContainer
            {
                CustomMinimumSize = new Vector2(40,40)
            };
            infoGrid.AddChild(holdContainer);

            var pointsLabel = new Label
            {
                Text = "Points: XYZ"
            };
            infoGrid.AddChild(pointsLabel);

            var highscoreLabel = new Label
            {
                Text = "1.Parzival - 🔑\n2. Hackerman - 1337\n3. Pothead - 420"
            };
            infoGrid.AddChild(highscoreLabel);

            var pauseBtn = new Button
            {
                Text = "Pause"
            };
            infoGrid.AddChild(pauseBtn);

            mainGrid.AddChild(infoGrid);

            Contents.AddChild(mainGrid);

            CanKeyboardFocus = true;
            RaisePauseMenu(true);
        }

        protected override void FocusExited()
        {
            //todo pause and grab keyboard focus on unpause
            _owner.SendAction(TetrisPlayerAction.Pause);
            RaisePauseMenu();
        }

        protected void RaisePauseMenu(bool onlyNewGame = false)
        {
            //todo show pause menu
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.MoveLeft)
            {
                _owner.SendAction(TetrisPlayerAction.StartLeft);
            }
            else if (args.Function == EngineKeyFunctions.MoveRight)
            {
                _owner.SendAction(TetrisPlayerAction.StartRight);
            }
            else if (args.Function == EngineKeyFunctions.MoveUp)
            {
                _owner.SendAction(TetrisPlayerAction.Rotate);
            }
            else if (args.Function == EngineKeyFunctions.Use)
            {
                _owner.SendAction(TetrisPlayerAction.CounterRotate);
            }
            else if (args.Function == EngineKeyFunctions.MoveDown)
            {
                _owner.SendAction(TetrisPlayerAction.SoftdropStart);
            }
            else if (args.Function == ContentKeyFunctions.WideAttack)
            {
                _owner.SendAction(TetrisPlayerAction.Harddrop);
            }
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {if (args.Function == EngineKeyFunctions.MoveLeft)
            {
                _owner.SendAction(TetrisPlayerAction.EndLeft);
            }
            else if (args.Function == EngineKeyFunctions.MoveRight)
            {
                _owner.SendAction(TetrisPlayerAction.EndRight);
            }else if (args.Function == EngineKeyFunctions.MoveDown)
            {
                _owner.SendAction(TetrisPlayerAction.SoftdropEnd);
            }
        }

        public void UpdateBlocks(TetrisBlock[] blocks)
        {
            _gameGrid.RemoveAllChildren();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Color c = Color.Transparent;
                    var matchingBlock = blocks.FirstOrNull(b => b.Position.X == x && b.Position.Y == y);
                    if (matchingBlock.HasValue)
                    {
                        c = matchingBlock.Value.Color switch
                        {
                            TetrisBlock.TetrisBlockColor.Red => Color.Red,
                            TetrisBlock.TetrisBlockColor.Orange => Color.Orange,
                            TetrisBlock.TetrisBlockColor.Yellow => Color.Yellow,
                            TetrisBlock.TetrisBlockColor.Green => Color.LimeGreen,
                            TetrisBlock.TetrisBlockColor.Blue => Color.Blue,
                            TetrisBlock.TetrisBlockColor.Purple => Color.Purple,
                            TetrisBlock.TetrisBlockColor.LightBlue => Color.LightBlue,
                            _ => Color.Olive //olive is error
                        };
                    }
                    _gameGrid.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = c},
                        CustomMinimumSize = new Vector2(10,10),
                        RectDrawClipMargin = 0
                    });
                }
            }
        }
    }
}
