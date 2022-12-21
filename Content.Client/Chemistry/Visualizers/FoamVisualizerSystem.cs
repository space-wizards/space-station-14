using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// Handles animating the foam once it is dissolving
/// </summary>
[UsedImplicitly]
public sealed class FoamVisualizerSystem : VisualizerSystem<FoamVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FoamVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // Return if the foam is not shutting down or it has no base or animation layers
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<bool>(uid, FoamVisuals.FoamShutdown, out var foamShutdown, args.Component) ||
            !foamShutdown ||
            !args.Sprite.LayerMapTryGet(FoamLayers.Animation, out var animationLayer) ||
            !args.Sprite.LayerMapTryGet(FoamLayers.Base, out var baseLayer))
        {
            return;
        }

        const string animationKey = "foam_animation";
        // Stop current animation and start a new one
        if (AnimationSystem.HasRunningAnimation(uid, animationKey))
        {
            AnimationSystem.Stop(uid, animationKey);
        }
        var animationComp = EnsureComp<AnimationPlayerComponent>(uid);
        var animationSprite = args.Sprite.LayerGetState(animationLayer);
        var animation = new Animation {
            Length = TimeSpan.FromSeconds(component.AnimationTime),
            AnimationTracks = {
                new AnimationTrackSpriteFlick {
                    LayerKey = FoamLayers.Base,
                    KeyFrames = {
                        new AnimationTrackSpriteFlick.KeyFrame(animationSprite, 0f)
                    }
                }
            }
        };

        args.Sprite[animationLayer].Visible = foamShutdown;
        args.Sprite[baseLayer].Visible = !foamShutdown;
        AnimationSystem.Play(animationComp, animation, animationKey);
    }
}
