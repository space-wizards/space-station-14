using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

[UsedImplicitly]
public sealed class ChemistryEffectVisualizerSystem : VisualizerSystem<ChemistryEffectVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ChemistryEffectVisualsComponent component, ref AppearanceChangeEvent args)
    {
        // TODO vapor foam animation
        base.OnAppearanceChange(uid, component, ref args);

        if (args.Sprite == null || !AppearanceSystem.TryGetData(uid, ChemistryEffectVisuals.Color, out var objColor, args.Component) || objColor is not Color color)
        {
            return;
        }

        args.Sprite.Color = color;

        // Handle foam shutdown
        if (AppearanceSystem.TryGetData(uid, ChemistryEffectVisuals.FoamShutdown, out var objState, args.Component) &&
            objState is bool foamState &&
            args.Sprite.LayerMapTryGet(ChemistryEffectLayers.Base, out var baseLayer) &&
            args.Sprite.LayerMapTryGet(ChemistryEffectLayers.Animation, out var animationLayer))
        {
            args.Sprite[animationLayer].Visible = (bool) objState;
            args.Sprite[baseLayer].Visible = !(bool) objState;

            var animationComp = EnsureComp<AnimationPlayerComponent>(uid);
            var animationSystem = EntityManager.System<AnimationPlayerSystem>();
            const string animationKey = "chem_effect_animation";
            // Stop the current chem_effect_animation animation and then start a new one
            if (animationSystem.HasRunningAnimation(animationComp, animationKey))
            {
                animationSystem.Stop(animationComp, animationKey);
            }

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

            animationSystem.Play(animationComp, animation, animationKey);
        }
    }
}
