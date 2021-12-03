using System;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Animations
{
    public static class ReusableAnimations
    {
        public static void AnimateEntityPickup(IEntity entity, EntityCoordinates initialPosition, Vector2 finalPosition)
        {
            var animatableClone = IoCManager.Resolve<IEntityManager>().SpawnEntity("clientsideclone", initialPosition);
            animatableClone.Name = entity.Name;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity.Uid, out SpriteComponent? sprite0))
            {
                Logger.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", entity.Name, nameof(SpriteComponent));
                return;
            }
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(animatableClone.Uid);
            sprite.CopyFrom(sprite0);

            var animations = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(animatableClone.Uid);
            animations.AnimationCompleted += (_) => {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(animatableClone.Uid);
            };

            animations.Play(new Animation
            {
                Length = TimeSpan.FromMilliseconds(125),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(TransformComponent),
                        Property = nameof(TransformComponent.LocalPosition),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(initialPosition.Position, 0),
                            new AnimationTrackProperty.KeyFrame(finalPosition, 0.125f)
                        }
                    }
                }
            }, "fancy_pickup_anim");
        }
    }
}
