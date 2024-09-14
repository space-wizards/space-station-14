using Content.Shared.Atmos.Components;
using Robust.Client.GameObjects;

public sealed class GasPipeSensorSystem : VisualizerSystem<GasPipeSensorComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, GasPipeSensorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, GasPipeSensorVisuals.State, out var isActive, args.Component))
            args.Sprite.LayerSetVisible(GasPipeSensorVisuals.Lights, isActive);
    }
}
