using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;

namespace Content.Shared.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    public override void SetupTabletop(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        SpawnPiece(BoardPrototype, tabletop.Position, tabletop, entityManager);
    }
}
