using Content.Shared.Chemistry.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.Chemistry.Visualizers;

public sealed class ReagentColorVisualizerSystem : VisualizerSystem<ReagentColorComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ReagentColorComponent component, ref AppearanceChangeEvent args)
    {
        if (AppearanceSystem.TryGetData(uid, ReagentColorVisuals.Color, out Color color, args.Component) &&
            TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.Color = color;
        }
    }
}
