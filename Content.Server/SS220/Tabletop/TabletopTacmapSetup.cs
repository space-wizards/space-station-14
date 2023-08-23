using Content.Server.Tabletop;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.SS220.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopTacmapSetup : TabletopSetup
{
    private const float SeparationInRow = 0.75f;

    public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
    {
        var tacmap = entityManager.SpawnEntity(BoardPrototype, session.Position.Offset(-1, 0));
        session.Entities.Add(tacmap);

        SpawnFigurines(session, entityManager, session.Position.Offset(-1, 0));
    }

    private void SpawnFigurines(TabletopSession session, IEntityManager entityManager, MapCoordinates offset)
    {
        var (mapId, x, y) = offset;

        // Nukies 1 row x 3
        for (var k = 0; k < 3; k++)
        {
            var figurine = entityManager.SpawnEntity("ToyFigurineNukieTabletop",
                new MapCoordinates(x + 9.55f, y + 7.25f - k * SeparationInRow, mapId));
            session.Entities.Add(figurine);
        }

        // Graytide 1 row x 3
        for (var k = 0; k < 3; k++)
        {
            var figurine = entityManager.SpawnEntity("ToyFigurinePassengerTabletop",
                new MapCoordinates(x + 9.55f + 1, y + 7.25f - k * SeparationInRow, mapId));
            session.Entities.Add(figurine);
        }

        var figurineSpaceDragon = entityManager.SpawnEntity("ToyFigurineSpaceDragonTabletop",
            new MapCoordinates(x + 9.55f, y + 4f, mapId));
        var figurineXenoQueen = entityManager.SpawnEntity("ToyFigurineQueenTabletop",
            new MapCoordinates(x + 9.55f + 1, y + 4f, mapId));
        var figurineClown = entityManager.SpawnEntity("ToyFigurineClownTabletop",
            new MapCoordinates(x + 9.55f, y + 3f, mapId));
        var figurineRatking = entityManager.SpawnEntity("ToyFigurineRatkingTabletop",
            new MapCoordinates(x + 9.55f + 1, y + 3f, mapId));

        session.Entities.Add(figurineSpaceDragon);
        session.Entities.Add(figurineXenoQueen);
        session.Entities.Add(figurineClown);
        session.Entities.Add(figurineRatking);

        // Security Officers, 2 rows x 3
        for (var i = 0; i < 2; i++)
        {
            for (var k = 0; k < 3; k++)
            {
                var figurine = entityManager.SpawnEntity("ToyFigurineSecurityTabletop",
                    new MapCoordinates(x + 9.55f + i, y + 2.0f - k * SeparationInRow, mapId));
                session.Entities.Add(figurine);
            }
        }

        var figurineHos = entityManager.SpawnEntity("ToyFigurineHeadOfSecurityTabletop",
            new MapCoordinates(x + 9.55f, y - 0.25f, mapId));
        var figurineDec = entityManager.SpawnEntity("ToyFigurineDetectiveTabletop",
            new MapCoordinates(x + 9.55f + 1, y - 0.25f, mapId));
        var figurineWarden = entityManager.SpawnEntity("ToyFigurineWardenTabletop",
            new MapCoordinates(x + 9.55f, y - 1.25f, mapId));
        var figurineCap = entityManager.SpawnEntity("ToyFigurineCaptainTabletop",
            new MapCoordinates(x + 9.55f + 1, y - 1.25f, mapId));

        session.Entities.Add(figurineHos);
        session.Entities.Add(figurineDec);
        session.Entities.Add(figurineWarden);
        session.Entities.Add(figurineCap);

        var figurineFlagA = entityManager.SpawnEntity("ToyFigurineFlagATabletop",
            new MapCoordinates(x + 9.55f, y - 3f, mapId));
        var figurineFlagB = entityManager.SpawnEntity("ToyFigurineFlagBTabletop",
            new MapCoordinates(x + 9.55f + 1, y - 3f, mapId));
        var figurineFlagC = entityManager.SpawnEntity("ToyFigurineFlagCTabletop",
            new MapCoordinates(x + 9.55f, y - 4f, mapId));
        var figurineFlagD = entityManager.SpawnEntity("ToyFigurineFlagDTabletop",
            new MapCoordinates(x + 9.55f + 1, y - 4f, mapId));
        var figurineFlagE = entityManager.SpawnEntity("ToyFigurineFlagETabletop",
            new MapCoordinates(x + 9.55f, y - 5f, mapId));
        var figurineFlagF = entityManager.SpawnEntity("ToyFigurineFlagFTabletop",
            new MapCoordinates(x + 9.55f + 1, y - 5f, mapId));

        session.Entities.Add(figurineFlagA);
        session.Entities.Add(figurineFlagB);
        session.Entities.Add(figurineFlagC);
        session.Entities.Add(figurineFlagD);
        session.Entities.Add(figurineFlagE);
        session.Entities.Add(figurineFlagF);

        var cigarSpent = entityManager.SpawnEntity("CigarSpentTabletop",
            new MapCoordinates(x + 5f, y + 8.15f, mapId));
        session.Entities.Add(cigarSpent);
    }
}
