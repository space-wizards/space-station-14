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

        private GridContainer _uiGrid;
        private Button _startButton;

        public TetrisArcadeMenu(TetrisArcadeBoundUserInterface owner)
        {
            _owner = owner;
            _uiGrid = new GridContainer
            {
                Columns = 10
            };

            _startButton = new Button();
            _startButton.OnPressed += StartButtonOnOnPressed;
            Contents.AddChild(_startButton);
            CanKeyboardFocus = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.MoveLeft)
            {
                _owner.SendAction(TetrisPlayerAction.Left);
            }
            else if (args.Function == EngineKeyFunctions.MoveRight)
            {
                _owner.SendAction(TetrisPlayerAction.Right);
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
        {
            if (args.Function == EngineKeyFunctions.MoveDown)
            {
                _owner.SendAction(TetrisPlayerAction.SoftdropEnd);
            }
        }

        private void StartButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            Contents.RemoveAllChildren();
            Contents.AddChild(_uiGrid);
            GrabKeyboardFocus();
            _owner.StartGame();
        }

        public void UpdateBlocks(TetrisBlock[] blocks)
        {
            _uiGrid.RemoveAllChildren();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Color c = Color.White;
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
                    _uiGrid.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = c},
                        CustomMinimumSize = new Vector2(5,5)
                    });
                }
            }
        }
    }
}
