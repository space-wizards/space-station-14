using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// Handles coloring and, if possible, animation of chemical effects like foam, smoke and sprays
/// </summary>
[UsedImplicitly]
public sealed class ChemistryEffectVisualizerSystem : VisualizerSystem<ChemistryEffectVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ChemistryEffectVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        // The sprite must exist, have a Base layer and color appearance data to be colored
        if (args.Sprite == null ||
            !args.Sprite.LayerMapTryGet(ChemistryEffectLayers.Base, out var baseLayer) ||
            !AppearanceSystem.TryGetData<Color>(uid, ChemistryEffectVisuals.Color, out var color, args.Component))
        {
            return;
        }

        args.Sprite.Color = color;

        // Handle everything with an animation layer
        if (args.Sprite.LayerMapTryGet(ChemistryEffectLayers.Animation, out var animationLayer))
        {
            var animationComp = EnsureComp<AnimationPlayerComponent>(uid);
            const string animationKey = "chem_effect_animation";
            var animationSprite = args.Sprite.LayerGetState(animationLayer);
            var animation = new Animation {
                Length = TimeSpan.FromSeconds(component.AnimationTime),
                AnimationTracks = {
                    new AnimationTrackSpriteFlick {
                        LayerKey = ChemistryEffectLayers.Base,
                        KeyFrames = {
                            new AnimationTrackSpriteFlick.KeyFrame(animationSprite, 0f)
                        }
                    }
                }
            };

            // Stop current animation and start a new one
            if (AnimationSystem.HasRunningAnimation(uid, animationKey))
            {
                AnimationSystem.Stop(uid, animationKey);
            }

            if (component.AnimateOnShutdown == false)
            {
                AnimationSystem.Play(animationComp, animation, animationKey);
                return;
            }

            // Handle foam shutdown specifically,
            if (AppearanceSystem.TryGetData<bool>(uid, ChemistryEffectVisuals.FoamShutdown, out var foamShutdown, args.Component) && foamShutdown == true)
            {
                args.Sprite[animationLayer].Visible = foamShutdown;
                args.Sprite[baseLayer].Visible = !foamShutdown;
                AnimationSystem.Play(animationComp, animation, animationKey);
            }
        }
    }
}
