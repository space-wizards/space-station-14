using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// Handles coloring of chemical effects like foam, smoke and sprays
/// </summary>
[UsedImplicitly]
public sealed class ColorReactionVisualizerSystem : VisualizerSystem<ColorReactionVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ColorReactionVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // The sprite must exist and have appearance data set to be colored
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<Color>(uid, ColorReactionVisuals.Color, out var color, args.Component))
        {
            return;
        }

        args.Sprite.Color = color;
    }
}
