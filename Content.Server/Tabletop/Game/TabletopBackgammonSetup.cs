using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop.Game;

[UsedImplicitly]
public sealed partial class TabletopBackgammonSetup : TabletopSetup
{
    [DataField]
    public EntProtoId BoardPrototype = default!;

    [DataField]
    public string WhitePiecePrototype = "WhiteTabletopPiece";

    [DataField]
    public string BlackPiecePrototype = "BlackTabletopPiece";

    protected override void SetupTabletop(Spawner spawner)
    {
        const float borderLengthX = 7.35f; //BORDER
        const float borderLengthY = 5.60f; //BORDER

        const float boardDistanceX = 1.25f;
        const float pieceDistanceY = 0.80f;

        float GetXPosition(float distanceFromSide, bool isLeftSide)
        {
            var pos = borderLengthX - (distanceFromSide * boardDistanceX);
            return isLeftSide ? -pos : pos;
        }

        float GetYPosition(float positionNumber, bool isTop)
        {
            var pos = borderLengthY - (pieceDistanceY * positionNumber);
            return isTop ? pos : -pos;
        }

        void AddPieces(
            float distanceFromSide,
            int numberOfPieces,
            bool isBlackPiece,
            bool isTop,
            bool isLeftSide)
        {
            foreach (var piece in Enumerable.Range(0, numberOfPieces))
            {
                spawner.Spawn(isBlackPiece ? BlackPiecePrototype : WhitePiecePrototype,
                    GetXPosition(distanceFromSide, isLeftSide),
                    GetYPosition(piece, isTop));
            }
        }

        spawner.Spawn(BoardPrototype, 0, 0);

        // Top left
        AddPieces(0, 5, true, true, true);
        // top middle left
        AddPieces(4, 3, false, true, true);
        // top middle right
        AddPieces(5, 5, false, true, false);
        // top far right
        AddPieces(0, 2, true, true, false);
        // bottom left
        AddPieces(0, 5, false, false, true);
        // bottom middle left
        AddPieces(4, 3, true, false, true);
        // bottom middle right
        AddPieces(5, 5, true, false, false);
        // bottom far right
        AddPieces(0, 2, false, false, false);
    }
}
