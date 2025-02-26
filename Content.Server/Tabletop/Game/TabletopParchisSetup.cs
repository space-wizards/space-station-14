using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop.Game;

[UsedImplicitly]
public sealed partial class TabletopParchisSetup : TabletopSetup
{
    [DataField]
    public EntProtoId BoardPrototype = default!;

    [DataField]
    public EntProtoId RedPiecePrototype = "RedTabletopPiece";

    [DataField]
    public EntProtoId GreenPiecePrototype = "GreenTabletopPiece";

    [DataField]
    public EntProtoId YellowPiecePrototype = "YellowTabletopPiece";

    [DataField]
    public EntProtoId BluePiecePrototype = "BlueTabletopPiece";

    protected override void SetupTabletop(Spawner spawner)
    {
        spawner.Spawn(BoardPrototype, 0, 0);

        const float x1 = 6.25f;
        const float x2 = 4.25f;

        const float y1 = 6.25f;
        const float y2 = 4.25f;

        // Red pieces.
        spawner.Spawn(RedPiecePrototype, -x1, -y1);
        spawner.Spawn(RedPiecePrototype, -x1, -y2);
        spawner.Spawn(RedPiecePrototype, -x2, -y1);
        spawner.Spawn(RedPiecePrototype, -x2, -y2);

        // Green pieces.
        spawner.Spawn(GreenPiecePrototype, x1, -y1);
        spawner.Spawn(GreenPiecePrototype, x1, -y2);
        spawner.Spawn(GreenPiecePrototype, x2, -y1);
        spawner.Spawn(GreenPiecePrototype, x2, -y2);

        // Yellow pieces.
        spawner.Spawn(YellowPiecePrototype, x1, y1);
        spawner.Spawn(YellowPiecePrototype, x1, y2);
        spawner.Spawn(YellowPiecePrototype, x2, y1);
        spawner.Spawn(YellowPiecePrototype, x2, y2);

        // Blue pieces.
        spawner.Spawn(BluePiecePrototype, -x1, y1);
        spawner.Spawn(BluePiecePrototype, -x1, y2);
        spawner.Spawn(BluePiecePrototype, -x2, y1);
        spawner.Spawn(BluePiecePrototype, -x2, y2);
    }
}
