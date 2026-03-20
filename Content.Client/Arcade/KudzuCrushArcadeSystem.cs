using Content.Client.Arcade.UI;
using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Systems;

namespace Content.Client.Arcade;

/// <inheritdoc/>
public sealed partial class KudzuCrushArcadeSystem : SharedKudzuCrushArcadeSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    protected override void CreateUIGrid(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (_ui.TryGetOpenUi<KudzuCrushArcadeBoundUserInterface>(ent.Owner, ArcadeUiKey.Key, out var bui))
        {
            bui.CreateGrid(ent.Comp.GridSize.X, ent.Comp.Grid);
        }
    }

    protected override void UpdateUIGridCell(Entity<KudzuCrushArcadeComponent> ent, int index, KudzuCrushArcadeCell cell)
    {
        if (_ui.TryGetOpenUi<KudzuCrushArcadeBoundUserInterface>(ent.Owner, ArcadeUiKey.Key, out var bui))
        {
            bui.UpdateGridCell(index, cell);
        }
    }
}
