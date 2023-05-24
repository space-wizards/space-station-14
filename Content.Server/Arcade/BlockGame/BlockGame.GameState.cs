using Content.Shared.Arcade;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Arcade.BlockGame;

public sealed partial class BlockGame
{
    // note: field is 10(0 -> 9) wide and 20(0 -> 19) high

    private bool LowerBoundCheck(Vector2i position)
    {
        return position.Y < 20;
    }
    private bool BorderCheck(Vector2i position)
    {
        return position.X >= 0 && position.X < 10;
    }
    private bool ClearCheck(Vector2i position)
    {   // O(n) whyyyyyyyy
        return _field.All(block => !position.Equals(block.Position));
    }

    private bool DropCheck(Vector2i position)
    {
        return LowerBoundCheck(position) && ClearCheck(position);
    }
    private bool MoveCheck(Vector2i position)
    {
        return BorderCheck(position) && ClearCheck(position);
    }
    private bool RotateCheck(Vector2i position)
    {
        return BorderCheck(position) && LowerBoundCheck(position) && ClearCheck(position);
    }

    /// <summary>
    /// The set of blocks that have landed in the field.
    /// </summary>
    private readonly List<BlockGameBlock> _field = new();


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

    /// <summary>
    /// The piece that is currently falling.
    /// </summary>
    private BlockGamePiece CurrentPiece
    {
        get => _internalCurrentPiece;
        set
        {
            _internalCurrentPiece = value;
            UpdateFieldUI();
        }
    }
    private BlockGamePiece _internalCurrentPiece = default!;


    /// <summary>
    /// The position of the falling piece.
    /// </summary>
    private Vector2i _currentPiecePosition;

    /// <summary>
    /// The rotation of the falling piece.
    /// </summary>
    private BlockGamePieceRotation _currentRotation;

    /// <summary>
    /// The rotation of the falling piece.
    /// </summary>
    private void TrySetRotation(BlockGamePieceRotation rotation)
    {
        if (!_running)
            return;

        if (!CurrentPiece.CanSpin)
            return;

        if (!CurrentPiece.Positions(_currentPiecePosition, rotation)
            .All(RotateCheck))
            return;

        _currentRotation = rotation;
        UpdateFieldUI();
    }


    /// <summary>
    /// The next piece that will be dispensed.
    /// </summary>
    private BlockGamePiece NextPiece
    {
        get => _internalNextPiece;
        set
        {
            _internalNextPiece = value;
            SendNextPieceUpdate();
        }
    }
    private BlockGamePiece _internalNextPiece = default!;


    /// <summary>
    /// The piece the player has chosen to hold in reserve.
    /// </summary>
    private BlockGamePiece? HeldPiece
    {
        get => _internalHeldPiece;
        set
        {
            _internalHeldPiece = value;
            SendHoldPieceUpdate();
        }
    }
    private BlockGamePiece? _internalHeldPiece = null;

    /// <summary>
    /// Prevents the player from holding the currently falling piece if true.
    /// Set true when a piece is held and set false when a new piece is created.
    /// Exists to prevent the player from swapping between two pieces forever and never actually letting the block fall.
    /// </summary>
    private bool _holdBlock = false;

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


    /// <summary>
    /// 
    /// </summary>
    private int Level
    {
        get => _internalLevel;
        set
        {
            if (_internalLevel == value)
                return;
            _internalLevel = value;
            SendLevelUpdate();
        }
    }
    private int _internalLevel = 0;


    /// <summary>
    /// 
    /// </summary>
    private int Points
    {
        get => _internalPoints;
        set
        {
            if (_internalPoints == value)
                return;
            _internalPoints = value;
            SendPointsUpdate();
        }
    }
    private int _internalPoints = 0;

    private void AddPoints(int amount)
    {
        if (amount == 0)
            return;

        Points += amount;
    }

    /// <summary>
    /// 
    /// </summary>
    private ArcadeSystem.HighScorePlacement? _highScorePlacement = null;
}
