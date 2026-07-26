using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;

namespace Content.Shared.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    public override void SetupTabletop(TabletopGameComponent tabletop, IEntityManager entityManager)
    {
        if (tabletop.Position is not { } position)
            return;

        SpawnPiece(BoardPrototype, position, tabletop, entityManager);
    }
}
