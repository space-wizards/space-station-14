using System;
using System.Linq;
using System.Net.Mime;
using Content.Client.GameObjects.Components.Arcade;
using Content.Client.Utility;
using Content.Shared.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using Content.Shared.Input;
using Robust.Client.Graphics;
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

        private PanelContainer _mainPanel;

        private VBoxContainer _gameRootContainer;
        private GridContainer _gameGrid;
        private GridContainer _nextBlockGrid;
        private GridContainer _holdBlockGrid;
        private Label _pointsLabel;
        private Button _pauseButton;

        private PanelContainer _menuRootContainer;
        private Button _unpauseButton;
        private Control _unpauseButtonMargin;
        private Button _newGameButton;
        private Button _scoreBoardButton;

        private PanelContainer _gameOverRootContainer;
        private Label _finalScoreLabel;
        private Button _finalNewGameButton;

        private bool _isPlayer = false;
        private bool _gameOver = false;

        public TetrisArcadeMenu(TetrisArcadeBoundUserInterface owner)
        {
            Title = "Tetris!";
            _owner = owner;

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var backgroundTexture = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");

            _mainPanel = new PanelContainer();

            SetupGameMenu(backgroundTexture);
            _mainPanel.AddChild(_gameRootContainer);

            SetupPauseMenu(backgroundTexture);

            SetupGameoverScreen(backgroundTexture);

            Contents.AddChild(_mainPanel);

            CanKeyboardFocus = true;
        }

        private void SetupGameoverScreen(Texture backgroundTexture)
        {
            var rootBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = new Color(0,0,0,83)
            };
            rootBack.SetPatchMargin(StyleBox.Margin.All, 10);
            _gameOverRootContainer = new PanelContainer
            {
                PanelOverride = rootBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            var innerBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = new Color(74,74,81,120)
            };
            innerBack.SetPatchMargin(StyleBox.Margin.All, 10);
            var menuInnerPanel = new PanelContainer
            {
                PanelOverride = innerBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            _gameOverRootContainer.AddChild(menuInnerPanel);

            var menuContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            menuContainer.AddChild(new Label{Text = "Gameover!",Align = Label.AlignMode.Center});
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});


            _finalScoreLabel = new Label{Align = Label.AlignMode.Center};
            menuContainer.AddChild(_finalScoreLabel);
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});

            _finalNewGameButton = new Button
            {
                Text = "New Game",
                TextAlign = Label.AlignMode.Center
            };
            _finalNewGameButton.OnPressed += (e) =>
            {
                _owner.SendAction(TetrisPlayerAction.NewGame);
            };
            menuContainer.AddChild(_finalNewGameButton);

            menuInnerPanel.AddChild(menuContainer);
        }

        private void SetupPauseMenu(Texture backgroundTexture)
        {
            var rootBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = new Color(0,0,0,83)
            };
            rootBack.SetPatchMargin(StyleBox.Margin.All, 10);
            _menuRootContainer = new PanelContainer
            {
                PanelOverride = rootBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            var innerBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = new Color(74,74,81,120)
            };
            innerBack.SetPatchMargin(StyleBox.Margin.All, 10);
            var menuInnerPanel = new PanelContainer
            {
                PanelOverride = innerBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            _menuRootContainer.AddChild(menuInnerPanel);


            var menuContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            _newGameButton = new Button
            {
                Text = "New Game",
                TextAlign = Label.AlignMode.Center
            };
            _newGameButton.OnPressed += (e) =>
            {
                _owner.SendAction(TetrisPlayerAction.NewGame);
            };
            menuContainer.AddChild(_newGameButton);
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});

            _scoreBoardButton = new Button
            {
                Text = "Scoreboard",
                TextAlign = Label.AlignMode.Center
            };
            //todo scoreBoardButton.OnPressed += (e) => ;
            menuContainer.AddChild(_scoreBoardButton);
            _unpauseButtonMargin = new Control {CustomMinimumSize = new Vector2(1, 10), Visible = false};
            menuContainer.AddChild(_unpauseButtonMargin);

            _unpauseButton = new Button
            {
                Text = "Unpause",
                TextAlign = Label.AlignMode.Center,
                Visible = false
            };
            _unpauseButton.OnPressed += (e) =>
            {
                _owner.SendAction(TetrisPlayerAction.Unpause);
            };
            menuContainer.AddChild(_unpauseButton);

            menuInnerPanel.AddChild(menuContainer);
        }

        public void SetUsability(bool isPlayer)
        {
            _isPlayer = isPlayer;
            UpdateUsability();
        }

        private void UpdateUsability()
        {
            _pauseButton.Disabled = !_isPlayer;
            _newGameButton.Disabled = !_isPlayer;
            _scoreBoardButton.Disabled = !_isPlayer;
            _unpauseButton.Disabled = !_isPlayer;
            _finalNewGameButton.Disabled = !_isPlayer;
        }

        private void SetupGameMenu(Texture backgroundTexture)
        {
            // building the game container
            _gameRootContainer = new VBoxContainer();

            _pointsLabel = new Label
            {
                Align = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            UpdatePoints(0);
            _gameRootContainer.AddChild(_pointsLabel);
            _gameRootContainer.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(1,10)
            });

            var gameBox = new HBoxContainer();
            gameBox.AddChild(SetupHoldBox(backgroundTexture));
            gameBox.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(10,1)
            });
            gameBox.AddChild(SetupGameGrid(backgroundTexture));
            gameBox.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(10,1)
            });
            gameBox.AddChild(SetupNextBox(backgroundTexture));

            _gameRootContainer.AddChild(gameBox);

            _gameRootContainer.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(1,10)
            });

            _pauseButton = new Button
            {
                Text = "Pause",
                TextAlign = Label.AlignMode.Center
            };
            _pauseButton.OnPressed += (e) => TryPause();
            _gameRootContainer.AddChild(_pauseButton);
        }

        private Control SetupGameGrid(Texture panelTex)
        {
            _gameGrid = new GridContainer
            {
                Columns = 10,
                HSeparationOverride = 1,
                VSeparationOverride = 1
            };
            UpdateBlocks(new TetrisBlock[0]);

            var back = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51"),
            };
            back.SetPatchMargin(StyleBox.Margin.All, 10);

            var gamePanel = new PanelContainer
            {
                PanelOverride = back,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 60
            };
            var backgroundPanel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat{BackgroundColor = Color.FromHex("#86868d")}
            };
            backgroundPanel.AddChild(_gameGrid);
            gamePanel.AddChild(backgroundPanel);
            return gamePanel;
        }

        private Control SetupNextBox(Texture panelTex)
        {
            var previewBack = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51")
            };
            previewBack.SetPatchMargin(StyleBox.Margin.All, 10);

            var grid = new GridContainer
            {
                Columns = 1,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 20
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

        private Control SetupHoldBox(Texture panelTex)
        {
            var previewBack = new StyleBoxTexture
            {
                Texture = panelTex,
                Modulate = Color.FromHex("#4a4a51")
            };
            previewBack.SetPatchMargin(StyleBox.Margin.All, 10);

            var grid = new GridContainer
            {
                Columns = 1,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsStretchRatio = 20
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
            if (!IsOpen) return;
            if(_gameOver) return;
            TryPause();
        }

        private void TryPause()
        {
            _owner.SendAction(TetrisPlayerAction.Pause);
        }

        public void SetStarted()
        {
            _gameOver = false;
            _unpauseButton.Visible = true;
            _unpauseButtonMargin.Visible = true;
        }

        public void SetScreen(TetrisMessages.TetrisScreen screen)
        {
            if (_gameOver) return;

            switch (screen)
            {
                case TetrisMessages.TetrisScreen.Game:
                    GrabKeyboardFocus();
                    CloseMenus();
                    _pauseButton.Disabled = !_isPlayer;
                    break;
                case TetrisMessages.TetrisScreen.Pause:
                    ReleaseKeyboardFocus();
                    CloseMenus();
                    _mainPanel.AddChild(_menuRootContainer);
                    _pauseButton.Disabled = true;
                    break;
                case TetrisMessages.TetrisScreen.Gameover:
                    _gameOver = true;
                    _pauseButton.Disabled = true;
                    ReleaseKeyboardFocus();
                    CloseMenus();
                    _mainPanel.AddChild(_gameOverRootContainer);
                    break;
            }
        }

        private void CloseMenus()
        {
            if(_mainPanel.Children.Contains(_menuRootContainer)) _mainPanel.RemoveChild(_menuRootContainer);
            if(_mainPanel.Children.Contains(_gameOverRootContainer)) _mainPanel.RemoveChild(_gameOverRootContainer);
        }

        public void SetGameoverPoints(int amount)
        {
            _finalScoreLabel.Text = $"Points: {amount}";
        }


        public void UpdatePoints(int points)
        {
            _pointsLabel.Text = $"Points: {points}";
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if(!_isPlayer) return;

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
        {
            if(!_isPlayer) return;

            if (args.Function == EngineKeyFunctions.MoveLeft)
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
            if (blocks.Length == 0) return;
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
            if (blocks.Length == 0) return;
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
