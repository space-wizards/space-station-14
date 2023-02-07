using Content.Shared.Smoking;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// Ensures entities with <see cref="SmokeVisualsComponent"/> have a color corresponding with their contained reagents.
/// </summary>
public sealed class SmokeVisualizerSystem : VisualizerSystem<SmokeVisualsComponent>
{
    /// <summary>
    /// Syncs the color of the smoke with the color of its contained reagents.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, SmokeVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<Color>(uid, SmokeVisuals.Color, out var color))
            return;
        args.Sprite.Color = color;
    }
}
