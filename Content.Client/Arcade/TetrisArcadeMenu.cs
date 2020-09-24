using System;
using System.Linq;
using Content.Client.GameObjects.Components.Arcade;
using Content.Client.Utility;
using Content.Shared.Arcade;
using Content.Shared.Input;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Serilog.Filters;

namespace Content.Client.Arcade
{
    public class TetrisArcadeMenu : SS14Window
    {
        private TetrisArcadeBoundUserInterface _owner;

        private GridContainer _gameGrid;
        private GridContainer _nextBlockGrid;
        private GridContainer _holdBlockGrid;
        private Button _startButton;
        private Label _pointsLabel;

        public TetrisArcadeMenu(TetrisArcadeBoundUserInterface owner)
        {
            Title = "Tetris!";
            _owner = owner;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            var mainGrid = new GridContainer
            {
                Columns = 3
            };

            mainGrid.AddChild(new CenterContainer
            {
                Children = { SetupHoldBox()}
            });

            mainGrid.AddChild(SetupGameGrid());

            var infoGrid = new GridContainer
            {
                Columns = 1
            };

            infoGrid.AddChild(SetupNextBox());

            _pointsLabel = new Label
            {
                Align = Label.AlignMode.Center
            };
            UpdatePoints(0);
            infoGrid.AddChild(_pointsLabel);

            var highscoreLabel = new Label
            {
                Text = "1.Parzival - 🔑\n2. Hackerman - 1337\n3. Pothead - 420",
                Align = Label.AlignMode.Center
            };
            infoGrid.AddChild(highscoreLabel);

            var pauseBtn = new Button
            {
                Text = "Pause",
                TextAlign = Label.AlignMode.Center,
                SizeFlagsVertical = SizeFlags.ShrinkEnd
            };
            infoGrid.AddChild(pauseBtn);

            mainGrid.AddChild(infoGrid);

            Contents.AddChild(mainGrid);

            CanKeyboardFocus = true;
            GrabKeyboardFocus();
            RaisePauseMenu(true);
        }

        private Control SetupGameGrid()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            _gameGrid = new GridContainer
            {
                Columns = 10,
                HSeparationOverride = 1,
                VSeparationOverride = 1
            };
            UpdateBlocks(new TetrisBlock[0]);

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51"),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);
            //back.SetPadding(StyleBox.Margin.All,10);

            var gamePanel = new PanelContainer
            {
                PanelOverride = back
            };
            var backgroundPanel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat{BackgroundColor = Color.FromHex("#86868d")}
            };
            backgroundPanel.AddChild(_gameGrid);
            gamePanel.AddChild(backgroundPanel);
            return gamePanel;
        }

        private Control SetupNextBox()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var previewBack = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51")
            };
            previewBack.SetPatchMargin(StyleBox.Margin.All, 10);

            var grid = new GridContainer
            {
                Columns = 1
            };

            var nextBlockPanel = new PanelContainer
            {
                PanelOverride = previewBack,
                CustomMinimumSize = new Vector2(65,65),
                SizeFlagsHorizontal = SizeFlags.None,
                SizeFlagsVertical = SizeFlags.None
            };
            var nextCenterContainer = new CenterContainer();
            _nextBlockGrid = new GridContainer
            {
                HSeparationOverride = 1,
                VSeparationOverride = 1
            };
            nextCenterContainer.AddChild(_nextBlockGrid);
            nextBlockPanel.AddChild(nextCenterContainer);
            grid.AddChild(nextBlockPanel);

            grid.AddChild(new Label{Text = "Next", Align = Label.AlignMode.Center});

            return grid;
        }

        private Control SetupHoldBox()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            var panelTex = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var previewBack = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51")
            };
            previewBack.SetPatchMargin(StyleBox.Margin.All, 10);

            var grid = new GridContainer
            {
                Columns = 1
            };

            var holdBlockPanel = new PanelContainer
            {
                PanelOverride = previewBack,
                CustomMinimumSize = new Vector2(65,65),
                SizeFlagsHorizontal = SizeFlags.None,
                SizeFlagsVertical = SizeFlags.None
            };
            var holdCenterContainer = new CenterContainer();
            _holdBlockGrid = new GridContainer
            {
                HSeparationOverride = 1,
                VSeparationOverride = 1
            };
            holdCenterContainer.AddChild(_holdBlockGrid);
            holdBlockPanel.AddChild(holdCenterContainer);
            grid.AddChild(holdBlockPanel);

            grid.AddChild(new Label{Text = "Hold", Align = Label.AlignMode.Center});

            return grid;
        }

        protected override void FocusExited()
        {
            //todo pause and grab keyboard focus on unpause
            _owner.SendAction(TetrisPlayerAction.Pause);
            RaisePauseMenu();
        }

        private void RaisePauseMenu(bool onlyNewGame = false)
        {
            //todo show pause menu
        }

        public void UpdatePoints(int points)
        {
            _pointsLabel.Text = $"Points: {points}";
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
                _owner.SendAction(TetrisPlayerAction.Hold);
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

        public void UpdateNextBlock(TetrisBlock[] blocks)
        {
            _nextBlockGrid.RemoveAllChildren();
            var columnCount = blocks.Max(b => b.Position.X) + 1;
            var rowCount = blocks.Max(b => b.Position.Y) + 1;
            _nextBlockGrid.Columns = columnCount;
            for (int y = 0; y < rowCount; y++)
            {
                for (int x = 0; x < columnCount; x++)
                {
                    var c = GetColorForPosition(blocks, x, y);
                    _nextBlockGrid.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = c},
                        CustomMinimumSize = new Vector2(10,10),
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        public void UpdateHeldBlock(TetrisBlock[] blocks)
        {
            _holdBlockGrid.RemoveAllChildren();
            var columnCount = blocks.Max(b => b.Position.X) + 1;
            var rowCount = blocks.Max(b => b.Position.Y) + 1;
            _holdBlockGrid.Columns = columnCount;
            for (int y = 0; y < rowCount; y++)
            {
                for (int x = 0; x < columnCount; x++)
                {
                    var c = GetColorForPosition(blocks, x, y);
                    _holdBlockGrid.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = c},
                        CustomMinimumSize = new Vector2(10,10),
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        public void UpdateBlocks(TetrisBlock[] blocks)
        {
            _gameGrid.RemoveAllChildren();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var c = GetColorForPosition(blocks, x, y);
                    _gameGrid.AddChild(new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = c},
                        CustomMinimumSize = new Vector2(10,10),
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        private Color GetColorForPosition(TetrisBlock[] blocks, int x, int y)
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

            return c;
        }
    }
}
