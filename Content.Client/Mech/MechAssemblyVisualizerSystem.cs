using Content.Shared.Mech;
using Robust.Client.GameObjects;

namespace Content.Client.Mech;

/// <summary>
/// Handles the sprite state changes while
/// constructing mech assemblies.
/// </summary>
public sealed class MechAssemblyVisualizerSystem : VisualizerSystem<MechAssemblyVisualsComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, MechAssemblyVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!_appearance.TryGetData(uid, MechAssemblyVisuals.State, out int stage, args.Component))
            return;

        var state = component.StatePrefix + stage;

        args.Sprite?.LayerSetState(0, state);
    }
}
