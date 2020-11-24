using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Arcade;
using Content.Client.Utility;
using Content.Shared.Arcade;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Arcade
{
    public class BlockGameMenu : SS14Window
    {
        private static readonly Color OverlayBackgroundColor = new(74,74,81,180);
        private static readonly Color OverlayShadowColor = new(0,0,0,83);

        private static readonly Vector2 BlockSize = new(15,15);

        private readonly BlockGameBoundUserInterface _owner;

        private readonly PanelContainer _mainPanel;

        private VBoxContainer _gameRootContainer;
        private GridContainer _gameGrid;
        private GridContainer _nextBlockGrid;
        private GridContainer _holdBlockGrid;
        private Label _pointsLabel;
        private Label _levelLabel;
        private Button _pauseButton;

        private PanelContainer _menuRootContainer;
        private Button _unpauseButton;
        private Control _unpauseButtonMargin;
        private Button _newGameButton;
        private Button _scoreBoardButton;

        private PanelContainer _gameOverRootContainer;
        private Label _finalScoreLabel;
        private Button _finalNewGameButton;

        private PanelContainer _highscoresRootContainer;
        private Label _localHighscoresLabel;
        private Label _globalHighscoresLabel;
        private Button _highscoreBackButton;

        private bool _isPlayer = false;
        private bool _gameOver = false;

        public BlockGameMenu(BlockGameBoundUserInterface owner)
        {
            Title = "Nanotrasen Block Game";
            _owner = owner;

            var resourceCache = IoCManager.Resolve<IResourceCache>();
            var backgroundTexture = resourceCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");

            _mainPanel = new PanelContainer();

            SetupGameMenu(backgroundTexture);
            _mainPanel.AddChild(_gameRootContainer);

            SetupPauseMenu(backgroundTexture);

            SetupGameoverScreen(backgroundTexture);

            SetupHighScoreScreen(backgroundTexture);

            Contents.AddChild(_mainPanel);

            CanKeyboardFocus = true;
        }


        private void SetupHighScoreScreen(Texture backgroundTexture)
        {
            var rootBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = OverlayShadowColor
            };
            rootBack.SetPatchMargin(StyleBox.Margin.All, 10);
            _highscoresRootContainer = new PanelContainer
            {
                PanelOverride = rootBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            var c = new Color(OverlayBackgroundColor.R,OverlayBackgroundColor.G,OverlayBackgroundColor.B,220);
            var innerBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = c
            };
            innerBack.SetPatchMargin(StyleBox.Margin.All, 10);
            var menuInnerPanel = new PanelContainer
            {
                PanelOverride = innerBack,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            };

            _highscoresRootContainer.AddChild(menuInnerPanel);

            var menuContainer = new VBoxContainer()
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };

            menuContainer.AddChild(new Label{Text = "Highscores"});
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});

            var highScoreBox = new HBoxContainer();

            _localHighscoresLabel = new Label
            {
                Align = Label.AlignMode.Center
            };
            highScoreBox.AddChild(_localHighscoresLabel);
            highScoreBox.AddChild(new Control{CustomMinimumSize = new Vector2(40,1)});
            _globalHighscoresLabel = new Label
            {
                Align = Label.AlignMode.Center
            };
            highScoreBox.AddChild(_globalHighscoresLabel);
            menuContainer.AddChild(highScoreBox);
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});
            _highscoreBackButton = new Button
            {
                Text = "Back",
                TextAlign = Label.AlignMode.Center
            };
            _highscoreBackButton.OnPressed += (e) => _owner.SendAction(BlockGamePlayerAction.Pause);
            menuContainer.AddChild(_highscoreBackButton);

            menuInnerPanel.AddChild(menuContainer);
        }

        private void SetupGameoverScreen(Texture backgroundTexture)
        {
            var rootBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = OverlayShadowColor
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
                Modulate = OverlayBackgroundColor
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
                _owner.SendAction(BlockGamePlayerAction.NewGame);
            };
            menuContainer.AddChild(_finalNewGameButton);

            menuInnerPanel.AddChild(menuContainer);
        }

        private void SetupPauseMenu(Texture backgroundTexture)
        {
            var rootBack = new StyleBoxTexture
            {
                Texture = backgroundTexture,
                Modulate = OverlayShadowColor
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
                Modulate = OverlayBackgroundColor
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
                _owner.SendAction(BlockGamePlayerAction.NewGame);
            };
            menuContainer.AddChild(_newGameButton);
            menuContainer.AddChild(new Control{CustomMinimumSize = new Vector2(1,10)});

            _scoreBoardButton = new Button
            {
                Text = "Scoreboard",
                TextAlign = Label.AlignMode.Center
            };
            _scoreBoardButton.OnPressed += (e) => _owner.SendAction(BlockGamePlayerAction.ShowHighscores);
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
                _owner.SendAction(BlockGamePlayerAction.Unpause);
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
            _highscoreBackButton.Disabled = !_isPlayer;
        }

        private void SetupGameMenu(Texture backgroundTexture)
        {
            // building the game container
            _gameRootContainer = new VBoxContainer();

            _levelLabel = new Label
            {
                Align = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _gameRootContainer.AddChild(_levelLabel);
            _gameRootContainer.AddChild(new Control
            {
                CustomMinimumSize = new Vector2(1,5)
            });

            _pointsLabel = new Label
            {
                Align = Label.AlignMode.Center,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
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
            UpdateBlocks(new BlockGameBlock[0]);

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
                CustomMinimumSize = BlockSize * 6.5f,
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
                CustomMinimumSize = BlockSize * 6.5f,
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
            _owner.SendAction(BlockGamePlayerAction.Pause);
        }

        public void SetStarted()
        {
            _gameOver = false;
            _unpauseButton.Visible = true;
            _unpauseButtonMargin.Visible = true;
        }

        public void SetScreen(BlockGameMessages.BlockGameScreen screen)
        {
            if (_gameOver) return;

            switch (screen)
            {
                case BlockGameMessages.BlockGameScreen.Game:
                    GrabKeyboardFocus();
                    CloseMenus();
                    _pauseButton.Disabled = !_isPlayer;
                    break;
                case BlockGameMessages.BlockGameScreen.Pause:
                    //ReleaseKeyboardFocus();
                    CloseMenus();
                    _mainPanel.AddChild(_menuRootContainer);
                    _pauseButton.Disabled = true;
                    break;
                case BlockGameMessages.BlockGameScreen.Gameover:
                    _gameOver = true;
                    _pauseButton.Disabled = true;
                    //ReleaseKeyboardFocus();
                    CloseMenus();
                    _mainPanel.AddChild(_gameOverRootContainer);
                    break;
                case BlockGameMessages.BlockGameScreen.Highscores:
                    //ReleaseKeyboardFocus();
                    CloseMenus();
                    _mainPanel.AddChild(_highscoresRootContainer);
                    break;
            }
        }

        private void CloseMenus()
        {
            if(_mainPanel.Children.Contains(_menuRootContainer)) _mainPanel.RemoveChild(_menuRootContainer);
            if(_mainPanel.Children.Contains(_gameOverRootContainer)) _mainPanel.RemoveChild(_gameOverRootContainer);
            if(_mainPanel.Children.Contains(_highscoresRootContainer)) _mainPanel.RemoveChild(_highscoresRootContainer);
        }

        public void SetGameoverInfo(int amount, int? localPlacement, int? globalPlacement)
        {
            var globalPlacementText = globalPlacement == null ? "-" : $"#{globalPlacement}";
            var localPlacementText = localPlacement == null ? "-" : $"#{localPlacement}";
            _finalScoreLabel.Text = $"Global: {globalPlacementText}\nLocal: {localPlacementText}\nPoints: {amount}";
        }

        public void UpdatePoints(int points)
        {
            _pointsLabel.Text = $"Points: {points}";
        }

        public void UpdateLevel(int level)
        {
            _levelLabel.Text = $"Level {level + 1}";
        }

        public void UpdateHighscores(List<BlockGameMessages.HighScoreEntry> localHighscores,
            List<BlockGameMessages.HighScoreEntry> globalHighscores)
        {
            var localHighscoreText = "Station:\n";
            var globalHighscoreText = "Nanotrasen:\n";
            for (int i = 0; i < 5; i++)
            {
                localHighscoreText += $"#{i + 1} " + (localHighscores.Count > i
                    ? $"{localHighscores[i].Name} - {localHighscores[i].Score}\n" : "??? - 0\n");
                globalHighscoreText += $"#{i + 1} " + (globalHighscores.Count > i
                    ? $"{globalHighscores[i].Name} - {globalHighscores[i].Score}\n" : "??? - 0\n");
            }

            _localHighscoresLabel.Text = localHighscoreText;
            _globalHighscoresLabel.Text = globalHighscoreText;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if(!_isPlayer || args.Handled) return;

            if (args.Function == ContentKeyFunctions.ArcadeLeft)
            {
                _owner.SendAction(BlockGamePlayerAction.StartLeft);
            }
            else if (args.Function == ContentKeyFunctions.ArcadeRight)
            {
                _owner.SendAction(BlockGamePlayerAction.StartRight);
            }
            else if (args.Function == ContentKeyFunctions.ArcadeUp)
            {
                _owner.SendAction(BlockGamePlayerAction.Rotate);
            }
            else if (args.Function == ContentKeyFunctions.Arcade3)
            {
                _owner.SendAction(BlockGamePlayerAction.CounterRotate);
            }
            else if (args.Function == ContentKeyFunctions.ArcadeDown)
            {
                _owner.SendAction(BlockGamePlayerAction.SoftdropStart);
            }
            else if (args.Function == ContentKeyFunctions.Arcade2)
            {
                _owner.SendAction(BlockGamePlayerAction.Hold);
            }
            else if (args.Function == ContentKeyFunctions.Arcade1)
            {
                _owner.SendAction(BlockGamePlayerAction.Harddrop);
            }
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if(!_isPlayer || args.Handled) return;

            if (args.Function == ContentKeyFunctions.ArcadeLeft)
            {
                _owner.SendAction(BlockGamePlayerAction.EndLeft);
            }
            else if (args.Function == ContentKeyFunctions.ArcadeRight)
            {
                _owner.SendAction(BlockGamePlayerAction.EndRight);
            }else if (args.Function == ContentKeyFunctions.ArcadeDown)
            {
                _owner.SendAction(BlockGamePlayerAction.SoftdropEnd);
            }
        }

        public void UpdateNextBlock(BlockGameBlock[] blocks)
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
                        CustomMinimumSize = BlockSize,
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        public void UpdateHeldBlock(BlockGameBlock[] blocks)
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
                        CustomMinimumSize = BlockSize,
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        public void UpdateBlocks(BlockGameBlock[] blocks)
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
                        CustomMinimumSize = BlockSize,
                        RectDrawClipMargin = 0
                    });
                }
            }
        }

        private Color GetColorForPosition(BlockGameBlock[] blocks, int x, int y)
        {
            Color c = Color.Transparent;
            var matchingBlock = blocks.FirstOrNull(b => b.Position.X == x && b.Position.Y == y);
            if (matchingBlock.HasValue)
            {
                c = matchingBlock.Value.GameBlockColor switch
                {
                    BlockGameBlock.BlockGameBlockColor.Red => Color.Red,
                    BlockGameBlock.BlockGameBlockColor.Orange => Color.Orange,
                    BlockGameBlock.BlockGameBlockColor.Yellow => Color.Yellow,
                    BlockGameBlock.BlockGameBlockColor.Green => Color.LimeGreen,
                    BlockGameBlock.BlockGameBlockColor.Blue => Color.Blue,
                    BlockGameBlock.BlockGameBlockColor.Purple => Color.Purple,
                    BlockGameBlock.BlockGameBlockColor.LightBlue => Color.LightBlue,
                    _ => Color.Olive //olive is error
                };
            }

            return c;
        }
    }
}
