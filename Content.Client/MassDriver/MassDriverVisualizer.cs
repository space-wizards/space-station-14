using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Content.Shared.MassDriver;

namespace Content.Client.MassDriver
{
    public sealed class MassDriverVisualizerSystem : VisualizerSystem<MassDriverComponent>
    {
        [Dependency] private readonly AnimationPlayerSystem _animationPlayerSystem = default!;

        static private readonly string AnimationKey = "visual";

        protected override void OnAppearanceChange(EntityUid uid, MassDriverComponent component, ref AppearanceChangeEvent args)
        {
            // TODO handle unpowered

            if (!args.Component.TryGetData(MassDriverVisuals.State, out MassDriverState state))
                return;

            switch (state)
            {
                case MassDriverState.Launching:
                    _animationPlayerSystem.Play(uid, new Animation()
                    {
                        Length = component.LaunchDelay,
                        AnimationTracks =
                        {
                            new AnimationTrackSpriteFlick
                            {
                                LayerKey = MassDriverVisualLayers.Base,
                                KeyFrames =
                                {
                                    new AnimationTrackSpriteFlick.KeyFrame(component.LaunchingState, 0f),
                                    new AnimationTrackSpriteFlick.KeyFrame(component.LaunchedState, (float) component.LaunchDelay.TotalMilliseconds),
                                }
                            }
                        }
                    }, AnimationKey);
                    break;

                case MassDriverState.Launched:
                    _animationPlayerSystem.Stop(uid, AnimationKey);
                    args.Sprite?.LayerSetState(MassDriverVisualLayers.Base, component.LaunchedState);
                    args.Sprite?.LayerSetAutoAnimated(MassDriverVisualLayers.Base, true);
                    break;

                case MassDriverState.Retracting:
                    _animationPlayerSystem.Play(uid, new Animation()
                    {
                        Length = component.RetractDelay,
                        AnimationTracks =
                        {
                            new AnimationTrackSpriteFlick
                            {
                                LayerKey = MassDriverVisualLayers.Base,
                                KeyFrames =
                                {
                                    new AnimationTrackSpriteFlick.KeyFrame(component.RetractingState, 0f),
                                    new AnimationTrackSpriteFlick.KeyFrame(component.ReadyState, (float) component.RetractDelay.TotalMilliseconds),
                                }
                            }
                        }
                    }, AnimationKey);
                    break;

                case MassDriverState.Ready:
                    _animationPlayerSystem.Stop(uid, AnimationKey);
                    args.Sprite?.LayerSetState(MassDriverVisualLayers.Base, component.ReadyState);
                    args.Sprite?.LayerSetAutoAnimated(MassDriverVisualLayers.Base, true);
                    break;
            }
        }
    }
}
