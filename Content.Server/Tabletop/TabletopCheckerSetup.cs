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

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates left)
        {
            static float GetOffset(float offset) => offset * 1f /* separation */;

            var pieces = new EntityUid[42];
            var pieceIndex = 0;

            void SpawnPieces(string protoId, MapCoordinates left)
            {
                var signum = (sbyte)(PrototypePieceWhite == protoId ? 1 : -1);

                for (var offsetY = 0; offsetY < 3; offsetY++)
                {
                    var checker = offsetY % 2;

                    // Invert pattern for black
                    if (signum == -1) checker = 1 - checker;

                    for (var offsetX = 0; offsetX < 8; offsetX += 2)
                    {
                        // Prevents an extra piece on the middle row
                        if (checker + offsetX > 8) continue;

                        pieces[pieceIndex] = entityManager.SpawnEntity(
                            protoId,
                            left.Offset(GetOffset(offsetX + checker), GetOffset(offsetY * signum))
                        );
                        pieceIndex++;
                    }
                }
            }

            SpawnPieces(PrototypePieceBlack, left);
            SpawnPieces(PrototypePieceWhite, left.Offset(0, GetOffset(-7)));

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
                    left.Offset(GetOffset(xOffsetBlack), GetOffset(step))
                );
                pieces[pieceIndex + 1] = entityManager.SpawnEntity(
                    PrototypeCrownWhite,
                    left.Offset(GetOffset(xOffsetWhite), GetOffset(step))
                );
                pieceIndex += 2;
            }

            // Spares
            for (var i = 0; i < 6; i++)
            {
                var step = -((Overlap * (NumCrowns + 2)) + (Overlap * i));
                pieces[pieceIndex] = entityManager.SpawnEntity(
                    PrototypePieceBlack,
                    left.Offset(GetOffset(xOffsetBlack), GetOffset(step))
                );
                pieces[pieceIndex] = entityManager.SpawnEntity(
                    PrototypePieceWhite,
                    left.Offset(GetOffset(xOffsetWhite), GetOffset(step))
                );
                pieceIndex += 2;
            }

            session.Entities.UnionWith(pieces);
        }
    }
}
