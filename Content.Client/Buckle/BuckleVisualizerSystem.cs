using Content.Shared.Buckle.Components;
using Content.Client.Rotation;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle;

public sealed class BuckleVisualizer : VisualizerSystem<BuckleComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BuckleComponent component, ref AppearanceChangeEvent args)
    {
        if (!args.Component.TryGetData<int>(StrapVisuals.RotationAngle, out var angle) ||
            !args.Component.TryGetData<bool>(BuckleVisuals.Buckled, out var buckled) ||
            !buckled ||
            args.Sprite == null)
        {
            return;
        }

        EntityManager.System<RotationVisualizerSystem>()
            .AnimateSpriteRotation(args.Sprite, Angle.FromDegrees(angle), 0.125f);
    }
}

