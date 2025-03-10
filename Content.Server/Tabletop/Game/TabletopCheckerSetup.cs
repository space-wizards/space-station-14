using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Tabletop.Game;

[UsedImplicitly]
public sealed partial class TabletopCheckerSetup : TabletopSetup
{
    [DataField]
    public EntProtoId BoardPrototype = default!;

    [DataField]
    public EntProtoId PrototypePieceWhite;

    [DataField]
    public EntProtoId PrototypeCrownWhite;

    [DataField]
    public EntProtoId PrototypePieceBlack;

    [DataField]
    public EntProtoId PrototypeCrownBlack;

    protected override void SetupTabletop(Spawner spawner)
    {
        spawner.Spawn(BoardPrototype, -1, 0);
        var em = spawner.WithRelativeSpawnPosition(-4.5f, 3.5f);

        // Pieces
        foreach (var offsetY in Enumerable.Range(0, 2))
        {
            var checker = offsetY % 2;

            for (var offsetX = 0; offsetX < 8; offsetX += 2)
            {
                // Prevents an extra piece on the middle row
                if (checker + offsetX > 8)
                    continue;

                em.Spawn(PrototypePieceBlack, offsetX + (1 - checker), offsetY * -1);
                em.Spawn(PrototypePieceWhite, offsetX + checker, offsetY - 7);
            }
        }

        const int numCrowns = 3;
        const float overlap = 0.25f;
        const float xOffset = 9f / 32;
        const float xOffsetBlack = 9 + xOffset;
        const float xOffsetWhite = 8 + xOffset;

        // Crowns
        foreach (var crown in Enumerable.Range(0, numCrowns))
        {
            var step = -(overlap * crown);
            em.Spawn(PrototypeCrownBlack, xOffsetBlack, step);
            em.Spawn(PrototypeCrownWhite, xOffsetWhite, step);
        }

        // Spares
        foreach (var spare in Enumerable.Range(0, 6))
        {
            var step = -((overlap * (numCrowns + 2)) + (overlap * spare));
            em.Spawn(PrototypePieceBlack, xOffsetBlack, step);
            em.Spawn(PrototypePieceWhite, xOffsetWhite, step);
        }
    }
}
