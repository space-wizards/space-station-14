using Content.Shared.Arcade.Components;
using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Systems;

namespace Content.Client.Arcade;

/// <inheritdoc/>
public sealed partial class KudzuCrushArcadeSystem : SharedKudzuCrushArcadeSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    protected override void UpdateUi(Entity<KudzuCrushArcadeComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        if (!_ui.TryGetOpenUi(ent.Owner, ArcadeUiKey.Key, out var bui))
            return;

        bui.Update();
    }
}
