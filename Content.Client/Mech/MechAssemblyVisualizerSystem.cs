using Content.Shared.Mech;
using Robust.Client.GameObjects;

namespace Content.Client.Mech;

public sealed class MechAssemblyVisualizerSystem : VisualizerSystem<MechAssemblyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MechAssemblyVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData(MechAssemblyVisuals.State, out int stage))
            return;

        var state = component.StatePrefix + stage;

        args.Sprite?.LayerSetState(0, state);
    }
}
