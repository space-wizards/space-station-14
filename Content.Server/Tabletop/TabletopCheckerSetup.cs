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

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            session.Entities.Add(
                entityManager.SpawnEntity(BoardPrototype, session.Position.Offset(-1, 0))
            );

            SpawnPieces(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft)
        {
            const float Separation = 1f;

            static float GetX(float x, float offset)
            {
                return x + (offset * Separation);
            }

            static float GetY(float y, float offset)
            {
                return y + (offset * Separation);
            }

            var (mapId, x, y) = topLeft;
            var pieces = new EntityUid[42];
            var pieceIndex = 0;

            void SpawnPieces(string protoId, MapCoordinates left)
            {
                var (mapId, x, y) = left;
                var signum = (sbyte)(PrototypePieceWhite == protoId ? 1 : -1);

                for (var offsetY = 0; offsetY < 3; offsetY++)
                {
                    var checker = offsetY % 2;
                    if (signum == -1) checker = 1 - checker; // Invert pattern for black

                    for (var offsetX = 0; offsetX < 8; offsetX += 2)
                    {
                        // Prevents an extra piece on the middle row
                        if (checker + offsetX > 8) continue;

                        pieces[pieceIndex] = entityManager.SpawnEntity(
                            protoId,
                            new MapCoordinates(
                                GetX(x, offsetX + checker),
                                GetY(y, offsetY * signum),
                                mapId)
                        );
                        pieceIndex++;
                    }
                }
            }

            SpawnPieces(PrototypePieceBlack, topLeft);
            SpawnPieces(PrototypePieceWhite, new MapCoordinates(x, GetY(y, -7), mapId));

            const int NumCrowns = 3;
            const float Overlap = 0.25f;
            const float xOffset = 9f / 32;
            const float xOffsetBlack = 9 + xOffset;
            const float xOffsetWhite = 8 + xOffset;

            // Crowns
            for (var i = 0; i < NumCrowns; i++)
            {
                var step = -(Overlap * i); // Overlap
                pieces[pieceIndex] = entityManager.SpawnEntity(
                    PrototypeCrownBlack,
                    new MapCoordinates(GetX(x, xOffsetBlack), GetY(y, step), mapId)
                );
                pieces[pieceIndex + 1] = entityManager.SpawnEntity(
                    PrototypeCrownWhite,
                    new MapCoordinates(GetX(x, xOffsetWhite), GetY(y, step), mapId)
                );
                pieceIndex += 2;
            }

            // Spares
            for (var i = 0; i < 6; i++)
            {
                var step = -((Overlap * (NumCrowns + 2)) + (Overlap * i));
                pieces[pieceIndex] = entityManager.SpawnEntity(
                    PrototypePieceBlack,
                    new MapCoordinates(GetX(x, xOffsetBlack), GetY(y, step), mapId)
                );
                pieces[pieceIndex] = entityManager.SpawnEntity(
                    PrototypePieceWhite,
                    new MapCoordinates(GetX(x, xOffsetWhite), GetY(y, step), mapId)
                );
                pieceIndex += 2;
            }

            session.Entities.UnionWith(pieces);
        }
    }
}
