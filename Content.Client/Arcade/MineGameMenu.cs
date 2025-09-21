using System.Numerics;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using static Content.Shared.Arcade.MineGameShared;

namespace Content.Client.Arcade;

/// <summary>
/// Client UI Window for 'Mine Game' Arcade Machine
/// </summary>
public sealed class MineGameMenu : DefaultWindow
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly GridContainer _gameGrid;
    private readonly BoxContainer _customBoardOptions;
    private readonly Label _timeLabel;
    private readonly Label _mineCountLabel;
    private readonly List<MineGameTile> _gameTiles;
    private TimeSpan _referenceTime;
    private int _boardWidth;
    private int _boardHeight;
    private bool _gameRunning;

    // Standard difficulty modes to display at the top for standard players
    private struct PresetDifficulty(string id, MineGameBoardSettings settings)
    {
        public readonly string Id = id;
        public readonly MineGameBoardSettings Settings = settings;
    }
    private static readonly PresetDifficulty[] PresetDifficulties =
    {
        new("easy", new(new(9, 9), 10)),
        new("medium", new(new(16, 16), 40)),
        new("hard", new(new(30, 16), 99))
    };

    public event Action<MineGameBoardSettings>? OnBoardSettingAction;
    public event Action<MineGameTileAction>? OnTileAction;

    #region Single Tile Control
    private sealed class MineGameTile : ContainerButton
    {
        // There is surely a more efficient way to do this than creating a grid of many buttons, but as of writing
        // the engine doesn't seem to really have built-in support for arbitrary in-UI tile grid rendering
        private readonly MineGameMenu _owner;
        private readonly AnimatedTextureRect _tileImage;
        private readonly int _i;
        public MineGameTileVisState CurrState;
        private Vector2i Pos => new(_i % _owner._boardWidth, _i / _owner._boardWidth);

        private SpriteSpecifier SpriteSpecifierFromTileState(MineGameTileVisState tileState)
        {
            string state = Enum.GetName(typeof(MineGameTileVisState), tileState)?.ToLower() ?? "mine";
            return new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/arcade_minegametile.rsi"), state);
        }
        public void UpdateImage()
        {
            _tileImage.SetFromSpriteSpecifier(SpriteSpecifierFromTileState(CurrState));
        }
        private void OnMineGameTilePressed(ButtonEventArgs _)
        {
            if (CurrState == MineGameTileVisState.Uncleared || CurrState >= MineGameTileVisState.ClearedEmpty)
            {
                // Tile should change for standard single-tile clear, but not chording
                if (CurrState == MineGameTileVisState.Uncleared)
                    CurrState = MineGameTileVisState.ClearedEmpty;

                _owner.OnTileAction?.Invoke(new MineGameTileAction(
                    Pos,
                    MineGameTileActionType.Clear
                ));
            }
        }
        private void PerformAction(MineGameTileActionType type)
        {
            _owner.OnTileAction?.Invoke(new MineGameTileAction(
                Pos,
                type
            ));
            UpdateImage();
        }
        private void TryToggleFlag()
        {
            if (!_owner._gameRunning)
                return;
            if (CurrState == MineGameTileVisState.Uncleared)
            {
                CurrState = MineGameTileVisState.Flagged;
                PerformAction(MineGameTileActionType.Flag);
            }
            else if (CurrState == MineGameTileVisState.Flagged)
            {
                CurrState = MineGameTileVisState.Uncleared;
                PerformAction(MineGameTileActionType.Unflag);
            }
        }
        private void OnMineGameTileKeyPress(GUIBoundKeyEventArgs eventArgs)
        {
            if (!_owner._gameRunning)
                return;
            if (eventArgs.Function == EngineKeyFunctions.UIRightClick)
                TryToggleFlag();
            else if (eventArgs.Function == ContentKeyFunctions.Arcade1)
            {
                TryToggleFlag();
                if (CurrState >= MineGameTileVisState.ClearedEmpty)
                    PerformAction(MineGameTileActionType.Clear);
            }
            UpdateImage();
        }
        public MineGameTile(MineGameMenu owner, int i)
        {
            _owner = owner;
            _i = i;
            MinSize = new Vector2i(16, 16);

            _tileImage = new AnimatedTextureRect { };
            _tileImage.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
            OnPressed += OnMineGameTilePressed;
            OnKeyBindDown += OnMineGameTileKeyPress;

            UpdateImage();
            AddChild(_tileImage);
        }
    }
    #endregion

    public MineGameMenu()
    {
        IoCManager.InjectDependencies(this);
        MinSize = new Vector2(160, 160);
        Title = Loc.GetString("minegame-menu-title");

        OnResized += OnResize;
        var gameContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        _gameGrid = new GridContainer
        {
            Columns = 1,
            HorizontalExpand = true,
            VerticalExpand = true,
            HSeparationOverride = 1,
            VSeparationOverride = 1,
            HorizontalAlignment = HAlignment.Center
        };
        _gameTiles = new List<MineGameTile>();

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center
        };

        // Set up difficulty options
        var boardOptionsPresets = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };
        foreach (var presetDifficulty in PresetDifficulties)
        {
            var presetNewGameButton = new Button()
            {
                Text = Loc.GetString($"minegame-menu-button-{presetDifficulty.Id}")
            };
            presetNewGameButton.OnPressed += _ => OnBoardSettingAction?.Invoke(presetDifficulty.Settings);
            boardOptionsPresets.AddChild(presetNewGameButton);
        }
        var newGameCustom = new Button()
        {
            Text = Loc.GetString("minegame-menu-button-custom-menu")
        };
        newGameCustom.OnPressed += ToggleCustomOptions;
        boardOptionsPresets.AddChild(newGameCustom);

        // (Toggleable visibility) Inputs for custom board width, height, mines
        _customBoardOptions = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            Visible = false
        };
        var widthTextEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("minegame-menu-field-custom-width"),
            HorizontalExpand = true,
            MinWidth = 60
        };
        var heightTextEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("minegame-menu-field-custom-height"),
            HorizontalExpand = true,
            MinWidth = 60
        };
        var minesTextEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("minegame-menu-field-custom-mines"),
            HorizontalExpand = true,
            MinWidth = 60
        };
        var applyCustomBoard = new Button { Text = Loc.GetString("minegame-menu-button-custom-apply") };
        applyCustomBoard.OnPressed += _ =>
        {
            int.TryParse(widthTextEdit.Text.ToString(), out var customWidth);
            int.TryParse(heightTextEdit.Text.ToString(), out var customHeight);
            int.TryParse(minesTextEdit.Text.ToString(), out var customMines);
            MineGameBoardSettings settings = new(
                new(customWidth, customHeight),
                customMines
            );
            OnBoardSettingAction?.Invoke(settings);
        };
        _customBoardOptions.AddChild(widthTextEdit);
        _customBoardOptions.AddChild(heightTextEdit);
        _customBoardOptions.AddChild(minesTextEdit);
        _customBoardOptions.AddChild(applyCustomBoard);

        // Add header
        _timeLabel = new Label { Align = Label.AlignMode.Center };
        header.AddChild(_timeLabel);
        header.AddChild(boardOptionsPresets);
        _mineCountLabel = new Label { Align = Label.AlignMode.Center };
        header.AddChild(_mineCountLabel);

        // Finish tree
        gameContainer.AddChild(header);
        gameContainer.AddChild(_customBoardOptions);
        gameContainer.AddChild(_gameGrid);
        Contents.AddChild(gameContainer);
    }

    private void ToggleCustomOptions(BaseButton.ButtonEventArgs args)
    {
        _customBoardOptions.Visible = !_customBoardOptions.Visible;
    }

    /// <summary>
    /// Updates game board with new, complete board/game visual state information
    /// </summary>
    public void UpdateBoard(int boardWidth, MineGameTileVisState[] tileStates, MineGameMetadata? metadata)
    {
        if (metadata != null)
        {
            _referenceTime = metadata.ReferenceTime;
            _gameRunning = metadata.Running;
            _mineCountLabel.Text = metadata.RemainingMines.ToString();
        }
        _gameGrid.Columns = boardWidth;
        int newBoardHeight = tileStates.Length / boardWidth;
        bool fixSize = _boardWidth != boardWidth || _boardHeight != newBoardHeight;
        _boardWidth = boardWidth;
        _boardHeight = newBoardHeight;

        for (var i = _gameTiles.Count; i < tileStates.Length; ++i)
        {
            // Add new tiles if we don't have enough pooled.
            var newTile = new MineGameTile(this, i);
            _gameTiles.Add(newTile);
            _gameGrid.AddChild(newTile);
        }
        for (var i = 0; i < tileStates.Length; ++i)
        {
            // Update state of existing tiles
            if (tileStates[i] != MineGameTileVisState.None)
            {
                _gameTiles[i].CurrState = tileStates[i];
                _gameTiles[i].UpdateImage();
            }
            _gameTiles[i].Visible = true;
        }
        for (var i = tileStates.Length; i < _gameTiles.Count; ++i)
        {
            // Hide any extra pooled tiles
            _gameTiles[i].Visible = false;
        }

        if (fixSize)
            OnResize();
    }
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_gameRunning)
        {
            var elapsedTime = _gameTiming.CurTime.Subtract(_referenceTime);
            _timeLabel.Text = elapsedTime.ToString("m\\:ss");
        }
        else
        {
            _timeLabel.Text = _referenceTime.ToString("m\\:ss");
        }
    }

    private void OnResize()
    {
        // Couldn't figure out another way to create a grid with scaling and evenly spaced (on both dims) children
        // Thus, the approximate size of each game tile is calculated manually and updated in this window resize hook
        Vector2 containerSize = new Vector2(Contents.Width, _gameGrid.Height);
        float sz = 16.0f;
        if (containerSize.X > containerSize.Y * _boardWidth / _boardHeight)
        {
            // excess width, constrain by height
            sz = containerSize.Y / _boardHeight;
        }
        else
        {
            // excess height, constrain by width
            sz = containerSize.X / _boardWidth;
        }
        sz *= .95f; // just in case; if resizing loop doesn't fire often enough ui can make tiles go off-ui a little
        Vector2 vecSz = new(sz, sz);
        foreach (var gameTile in _gameTiles)
        {
            gameTile.MinSize = gameTile.SetSize = vecSz;
        }
    }
}
