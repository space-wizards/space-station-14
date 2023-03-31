using System;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Animations
{
    [RegisterComponent]
    public sealed class AnimationsTestComponent : Component
    {
        protected override void Initialize()
        {
            base.Initialize();

            var animations = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(Owner);
            animations.Play(new Animation
            {
                Length = TimeSpan.FromSeconds(20),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(TransformComponent),
                        Property = nameof(TransformComponent.LocalRotation),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(Angle.Zero, 0),
                            new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(1440), 20)
                        }
                    },
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = "layer/0/texture",
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame("Objects/toolbox_r.png", 0),
                            new AnimationTrackProperty.KeyFrame("Objects/Toolbox_b.png", 5),
                            new AnimationTrackProperty.KeyFrame("Objects/Toolbox_y.png", 5),
                            new AnimationTrackProperty.KeyFrame("Objects/toolbox_r.png", 5),
                        }
                    }
                }
            }, "yes");
        }
    }
}
