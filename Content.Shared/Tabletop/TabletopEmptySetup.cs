using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up an empty board at a given position.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    public override void SetupPieces(Entity<TabletopGameComponent> tabletop, EntityUid board, EntityManager entityManager)
    {
    }
}
