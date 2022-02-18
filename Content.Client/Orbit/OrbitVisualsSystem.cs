using Content.Shared.Follower;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Orbit;

// Not a visualizer system as we do not actually have any need for appearance events.
public sealed class OrbitVisualsSystem : EntitySystem
{
    private readonly string _orbitAnimationKey = "orbiting";
    private readonly string _orbitStopKey = "orbiting_stop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitVisualsComponent, StartedFollowingEntityEvent>(OnStartedFollowingEntity);
        SubscribeLocalEvent<OrbitVisualsComponent, StoppedFollowingEntityEvent>(OnStoppedFollowingEntity);
        SubscribeLocalEvent<OrbitVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnStartedFollowingEntity(EntityUid uid, OrbitVisualsComponent component, StartedFollowingEntityEvent args)
    {
        if (!TryComp(uid, out ISpriteComponent? sprite))
            return;

        var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);

        if (animationPlayer.HasRunningAnimation(_orbitAnimationKey))
        {
            animationPlayer.Stop(_orbitAnimationKey);
        }

        animationPlayer.Play(GetOrbitAnimation(component, sprite), _orbitAnimationKey);
    }

    private void OnStoppedFollowingEntity(EntityUid uid, OrbitVisualsComponent component, StoppedFollowingEntityEvent args)
    {
        if (!TryComp(uid, out ISpriteComponent? sprite))
            return;

        var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
        animationPlayer.Stop(_orbitAnimationKey);
        animationPlayer.Play(GetStopAnimation(component, sprite), _orbitStopKey);
    }

    private void OnAnimationCompleted(EntityUid uid, OrbitVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key == _orbitAnimationKey)
        {
            if(EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer)
               && EntityManager.TryGetComponent(uid, out ISpriteComponent? sprite))

            animationPlayer.Play(GetOrbitAnimation(component, sprite), _orbitAnimationKey);
        }
    }

    private Animation GetOrbitAnimation(OrbitVisualsComponent component, ISpriteComponent sprite)
    {
        var length = component.OrbitLength;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(ISpriteComponent),
                    Property = nameof(ISpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, -0.5f), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.5f, 0), length / 4),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, 0.5f), length / 2),
                        new AnimationTrackProperty.KeyFrame(new Vector2(-0.5f, 0), length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(ISpriteComponent),
                    Property = nameof(ISpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0f),
                        new AnimationTrackProperty.KeyFrame(new Angle(2 * Math.PI), length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }

    private Animation GetStopAnimation(OrbitVisualsComponent component, ISpriteComponent sprite)
    {
        var length = 2.0f;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(ISpriteComponent),
                    Property = nameof(ISpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, 0), length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(ISpriteComponent),
                    Property = nameof(ISpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }
}
