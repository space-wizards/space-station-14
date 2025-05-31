using Content.Shared.Mech;
using Robust.Client.GameObjects;

namespace Content.Client.Mech;

/// <summary>
/// Handles the sprite state changes while
/// constructing mech assemblies.
/// </summary>
public sealed class MechAssemblyVisualizerSystem : VisualizerSystem<MechAssemblyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MechAssemblyVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, MechAssemblyVisuals.State, out var stage, args.Component))
            return;

        var state = component.StatePrefix + stage;
        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, state);
    }
}
