using Content.Shared.Vapor;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers.Vapor;

// TODO merge this with FoamVisualsSystem
[UsedImplicitly]
public sealed class VaporVisualizerSystem : VisualizerSystem<VaporVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VaporVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData<bool>(VaporVisuals.State, out var state) || !state ||
            !args.Component.TryGetData<Color>(VaporVisuals.Color, out var color) ||
            args.Sprite == null ||
            !TryComp<AnimationPlayerComponent>(args.Component.Owner, out var animation))
        {
            return;
        }

        const string animationKey = "flick_animation";
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
                    LayerKey = VaporVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(component.AnimationState, 0f) }
                }
            }
        }, animationKey);

        args.Sprite.Color = color;
    }
}
