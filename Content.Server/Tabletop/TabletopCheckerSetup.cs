using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed class TabletopCheckerSetup : TabletopSetup
    {

        [DataField("prototypePieceWhite", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrototypePieceWhite = default!;

        [DataField("prototypeCrownWhite", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrototypeCrownWhite = default!;

        [DataField("prototypePieceBlack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrototypePieceBlack = default!;

        [DataField("prototypeCrownBlack", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrototypeCrownBlack = default!;

        private const float Separation = 1f;

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var checkerboard = entityManager.SpawnEntity(BoardPrototype, session.Position.Offset(-1, 0));

            session.Entities.Add(checkerboard);

            SpawnPieces(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft)
        {
            var (mapId, x, y) = topLeft;
            var pieces = new EntityUid[42];
            var pieceIndex = 0;

            void SpawnPieces(string protoId, MapCoordinates left)
            {
                var (mapId, x, y) = left;
                var reversed = (PrototypePieceWhite == protoId) ? 1 : -1;

                for (var i = 0; i < 3; i++)
                {
                    var x_offset = i % 2;
                    if (reversed == -1) x_offset = 1 - x_offset; // Flips it

                    for (var j = 0; j < 8; j += 2)
                    {
                        // Prevents an extra piece on the middle row
                        if (x_offset + j > 8) continue;

                        pieces[pieceIndex] = entityManager.SpawnEntity(protoId,
                            new MapCoordinates(x + (j + x_offset) * Separation, y + i * reversed * Separation, mapId)
                        );
                        pieceIndex++;
                    }
                }
            }

            SpawnPieces(PrototypePieceBlack, topLeft);
            SpawnPieces(PrototypePieceWhite, new MapCoordinates(x, y - 7 * Separation, mapId));

            // Crowns
            for (var i = 1; i < 4; i++)
            {
                pieces[pieceIndex] = entityManager.SpawnEntity(PrototypeCrownBlack,
                    new MapCoordinates(x + 9 * Separation + 9f / 32, y - (i - 1) * Separation, mapId)
                );
                pieces[pieceIndex + 1] = entityManager.SpawnEntity(PrototypeCrownWhite,
                    new MapCoordinates(x + 8 * Separation + 9f / 32, y - (i - 1) * Separation, mapId)
                );
                pieceIndex += 2;
            }

            // Spares
            for (var i = 1; i < 7; i++)
            {
                var step = 3 + 0.25f * (i - 1);
                pieces[pieceIndex] = entityManager.SpawnEntity(PrototypePieceBlack,
                  new MapCoordinates(x + 9 * Separation + 9f / 32, y - step * Separation, mapId)
                );
                pieces[pieceIndex] = entityManager.SpawnEntity(PrototypePieceWhite,
                  new MapCoordinates(x + 8 * Separation + 9f / 32, y - step * Separation, mapId)
                );
                pieceIndex += 2;
            }

            session.Entities.UnionWith(pieces);
        }
    }
}
