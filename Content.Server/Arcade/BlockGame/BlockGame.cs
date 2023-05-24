using Content.Shared.Arcade;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly ArcadeSystem _arcadeSystem = default!;
    private readonly UserInterfaceSystem _uiSystem = default!;

    private readonly EntityUid _owner = default!;

    public bool Started { get; private set; } = false;
    private bool _running = false;
    private bool _gameOver = false;
    private bool Paused => !(Started && _running);
    private const float SoftDropModifier = 0.1f;
    private float Speed => -0.03f * Level + 1 * (!_softDropPressed ? 1 : SoftDropModifier);

    public BlockGame(EntityUid owner)
    {
        IoCManager.InjectDependencies(this);
        _arcadeSystem = _entityManager.System<ArcadeSystem>();
        _uiSystem = _entityManager.System<UserInterfaceSystem>();

        _owner = owner;
        _allBlockGamePieces = (BlockGamePieceType[]) Enum.GetValues(typeof(BlockGamePieceType));
        _internalNextPiece = GetRandomBlockGamePiece(_random);
        InitializeNewBlock();
    }

    public void StartGame()
    {
        SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));

        FullUpdate();

        Started = true;
        _running = true;
        _gameOver = false;
    }

    private bool IsGameOver => _field.Any(block => block.Position.Y == 0);

    private void InvokeGameover()
    {
        _running = false;
        _gameOver = true;

        if (_entityManager.TryGetComponent<BlockGameArcadeComponent>(_owner, out var cabinet)
        && _entityManager.TryGetComponent<MetaDataComponent>(cabinet.Player?.AttachedEntity, out var meta))
        {
            _highScorePlacement = _arcadeSystem.RegisterHighScore(meta.EntityName, Points);
            SendHighscoreUpdate();
        }
        SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement));
    }

    public void GameTick(float frameTime)
    {
        if (!_running)
            return;

        InputTick(frameTime);

        FieldTick(frameTime);
    }

    private float _accumulatedFieldFrameTime;
    private void FieldTick(float frameTime)
    {
        _accumulatedFieldFrameTime += frameTime;

        // Speed goes negative sometimes. uhhhh max() it I guess!!!
        var checkTime = Math.Max(0.03f, Speed);

        while (_accumulatedFieldFrameTime >= checkTime)
        {
            if (_softDropPressed)
                AddPoints(1);

            InternalFieldTick();

            _accumulatedFieldFrameTime -= checkTime;
        }
    }

    private void InternalFieldTick()
    {
        if (CurrentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
            .All(DropCheck))
        {
            _currentPiecePosition = _currentPiecePosition.AddToY(1);
        }
        else
        {
            var blocks = CurrentPiece.Blocks(_currentPiecePosition, _currentRotation);
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

        UpdateFieldUI();
    }

    private void CheckField()
    {
        var pointsToAdd = 0;
        var consecutiveLines = 0;
        var clearedLines = 0;
        for (var y = 0; y < 20; y++)
        {
            if (CheckLine(y))
            {
                //line was cleared
                y--;
                consecutiveLines++;
                clearedLines++;
            }
            else if (consecutiveLines != 0)
            {
                var mod = consecutiveLines switch
                {
                    1 => 40,
                    2 => 100,
                    3 => 300,
                    4 => 1200,
                    _ => 0
                };
                pointsToAdd += mod * (Level + 1);
            }
        }

        ClearedLines += clearedLines;
        AddPoints(pointsToAdd);
    }

    private bool CheckLine(int y)
    {
        for (var x = 0; x < 10; x++)
        {
            if (!_field.Any(b => b.Position.X == x && b.Position.Y == y))
                return false;
        }

        //clear line
        _field.RemoveAll(b => b.Position.Y == y);
        //move everything down
        FillLine(y);

        return true;
    }

    private void FillLine(int y)
    {
        for (var c_y = y; c_y > 0; c_y--)
        {
            for (var j = 0; j < _field.Count; j++)
            {
                if (_field[j].Position.Y != c_y - 1)
                    continue;

                _field[j] = new BlockGameBlock(_field[j].Position.AddToY(1), _field[j].GameBlockColor);
            }
        }
    }

    private void InitializeNewBlock()
    {
        InitializeNewBlock(NextPiece);
        NextPiece = GetRandomBlockGamePiece(_random);
        _holdBlock = false;

        SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(NextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
    }

    private void InitializeNewBlock(BlockGamePiece piece)
    {
        _currentPiecePosition = new Vector2i(5, 0);

        _currentRotation = BlockGamePieceRotation.North;

        CurrentPiece = piece;
        UpdateFieldUI();
    }

    private void HoldPiece()
    {
        if (!_running)
            return;
        if (_holdBlock)
            return;

        var tempHeld = HeldPiece;
        HeldPiece = CurrentPiece;
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
        var spacesDropped = 0;
        while (CurrentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
            .All(DropCheck))
        {
            _currentPiecePosition = _currentPiecePosition.AddToY(1);
            spacesDropped++;
        }
        AddPoints(spacesDropped * 2);

        InternalFieldTick();
    }
}
