using Content.Server.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class TabletopSetup
    {
        /// <summary>
        ///     Method for setting up a tabletop. Use this to spawn the board and pieces, etc.
        ///     Make sure you add every entity you create to the Entities hashset in the session.
        /// </summary>
        /// <param name="game">Tabletop game component. You'll want to parse properties in this object.</param>
        /// <param name="session">Tabletop session to set up. You'll want to grab the tabletop center position here for spawning entities.</param>
        /// <param name="entityManager">Dependency that can be used for spawning entities.</param>
        public void SetupTabletop(TabletopGameComponent game, TabletopSession session, IEntityManager entityManager)
        {
            foreach (PieceGrid grid in game.PieceGrids) SpawnGrid(session, entityManager, grid,game.Pieces);
            SpawnFreePieces(game, session, entityManager);
        }

        private void SpawnGrid(TabletopSession session, IEntityManager entityManager, PieceGrid grid, Dictionary<char, string> pieceMapping)
        {
            var (mapId, x, y) = session.Position.Offset(grid.StartingPosition);
            string? pieceId = "";

            var (ix, iy) = (x, y);

            // Spawn pieces in StartingPiecePlacement
            foreach (char c in grid.PieceString)
            {
                if (pieceMapping.TryGetValue(c, out pieceId))
                {
                    EntityUid tempQualifier = entityManager.SpawnEntity(pieceId, new MapCoordinates(ix, iy, mapId));
                    session.Entities.Add(tempQualifier);
                    ix += grid.Separation.X;
                    continue;
                }
                if (Char.IsDigit(c))
                {
                    ix += (c - '0') * grid.Separation.X;
                    continue;
                }
                if (c == '/')
                {
                    ix = x;
                    iy += grid.Separation.Y;
                    continue;
                }
            }
        }

        private void SpawnFreePieces(TabletopGameComponent game, TabletopSession session, IEntityManager entityManager)
        {
            var (mapId, x, y) = session.Position;

            // Spawn additional pieces
            foreach (FreePiece piece in game.FreePieces)
            {
                float posX = x + piece.Position.X;
                float posY = y + piece.Position.Y;
                EntityUid tempQualifier = entityManager.SpawnEntity(piece.PiecePrototype, new MapCoordinates(posX, posY, mapId));
                session.Entities.Add(tempQualifier);
            }
        }
    }
}
