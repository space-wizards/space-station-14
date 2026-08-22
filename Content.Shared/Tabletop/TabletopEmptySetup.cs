using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Tabletop;

/// <summary>
/// A class to set up an empty board at a given position.
/// </summary>
[UsedImplicitly]
public sealed partial class TabletopEmptySetup : TabletopSetup
{
    /// <inheritdoc />
    public override void SetupTabletop(Entity<TabletopGameComponent> tabletop, MapCoordinates coordinates, EntityManager entityManager)
    {
        tabletop.Comp.Board = entityManager.SpawnEntity(BoardPrototype, coordinates);
    }
}
