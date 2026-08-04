using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Tabletop;

/// <summary>
///
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    /// <inheritdoc />
    public override void SetupTabletop(TabletopGameComponent tabletop, MapCoordinates coordinates, IEntityManager entityManager)
    {
        tabletop.Board = entityManager.SpawnEntity(BoardPrototype, coordinates);
    }
}
