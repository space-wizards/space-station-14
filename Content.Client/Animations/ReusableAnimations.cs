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
            string val = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityName;
            IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(animatableClone).EntityName = val;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SpriteComponent? sprite0))
            {
                Logger.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityName, nameof(SpriteComponent));
                return;
            }
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(animatableClone);
            sprite.CopyFrom(sprite0);

            var animations = IoCManager.Resolve<IEntityManager>().GetComponent<AnimationPlayerComponent>(animatableClone);
            animations.AnimationCompleted += (_) => {
                IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) animatableClone);
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
