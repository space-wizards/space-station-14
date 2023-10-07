using System.Numerics;
using Robust.Shared.Spawners;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Animations
{
    public static class ReusableAnimations
    {
        public static void AnimateEntityPickup(EntityUid entity, EntityCoordinates initialCoords, Vector2 finalPosition, Angle initialAngle, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);

            if (entMan.Deleted(entity) || !initialCoords.IsValid(entMan))
                return;

            var metadata = entMan.GetComponent<MetaDataComponent>(entity);

            if (entMan.IsPaused(entity, metadata))
                return;

            var animatableClone = entMan.SpawnEntity("clientsideclone", initialCoords);
            string val = entMan.GetComponent<MetaDataComponent>(entity).EntityName;
            entMan.System<MetaDataSystem>().SetEntityName(animatableClone, val);

            if (!entMan.TryGetComponent(entity, out SpriteComponent? sprite0))
            {
                Logger.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", entMan.GetComponent<MetaDataComponent>(entity).EntityName, nameof(SpriteComponent));
                return;
            }
            var sprite = entMan.GetComponent<SpriteComponent>(animatableClone);
            sprite.CopyFrom(sprite0);
            sprite.Visible = true;

            var animations = entMan.GetComponent<AnimationPlayerComponent>(animatableClone);
            animations.AnimationCompleted += (_) =>
            {
                entMan.DeleteEntity(animatableClone);
            };

            var despawn = entMan.EnsureComponent<TimedDespawnComponent>(animatableClone);
            despawn.Lifetime = 0.25f;
            entMan.System<SharedTransformSystem>().SetLocalRotationNoLerp(animatableClone, initialAngle);

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
                            new AnimationTrackProperty.KeyFrame(initialCoords.Position, 0),
                            new AnimationTrackProperty.KeyFrame(finalPosition, 0.125f)
                        }
                    },
                }
            }, "fancy_pickup_anim");
        }
    }
}
