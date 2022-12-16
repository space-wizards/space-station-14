using Content.Shared.Foam;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers.Foam;

[UsedImplicitly]
public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FoamVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData<bool>(FoamVisuals.State, out var state) || !state ||
            !args.Component.TryGetData<Color>(FoamVisuals.Color, out var color) ||
            args.Sprite == null ||
            !TryComp<AnimationPlayerComponent>(args.Component.Owner, out var animation))
        {
            return;
        }

        const string animationKey = "foamdissolve_animation";
        if (animation.HasRunningAnimation(animationKey))
        {
            animation.Stop(animationKey);
        }

        animation.Play(new Animation
        {
            Length = TimeSpan.FromSeconds(component.AnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = FoamVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(component.AnimationState, 0f) }
                }
            }
        }, animationKey);

        args.Sprite.Color = color;
    }
}
