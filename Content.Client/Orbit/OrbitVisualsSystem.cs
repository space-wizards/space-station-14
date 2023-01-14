using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;

namespace Content.Client.Orbit;

public sealed class OrbitVisualsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private readonly string _orbitAnimationKey = "orbiting";
    private readonly string _orbitStopKey = "orbiting_stop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<OrbitVisualsComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<OrbitVisualsComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnComponentInit(EntityUid uid, OrbitVisualsComponent component, ComponentInit args)
    {
        component.OrbitDistance =
            _robustRandom.NextFloat(0.75f * component.OrbitDistance, 1.25f * component.OrbitDistance);

        component.OrbitLength = _robustRandom.NextFloat(0.5f * component.OrbitLength, 1.5f * component.OrbitLength);

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = Direction.South;
        }

        var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
        if (animationPlayer.HasRunningAnimation(_orbitAnimationKey))
            return;

        if (animationPlayer.HasRunningAnimation(_orbitStopKey))
        {
            animationPlayer.Stop(_orbitStopKey);
        }

        animationPlayer.Play(GetOrbitAnimation(component), _orbitAnimationKey);
    }

    private void OnComponentRemove(EntityUid uid, OrbitVisualsComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.EnableDirectionOverride = false;

        var animationPlayer = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
        if (animationPlayer.HasRunningAnimation(_orbitAnimationKey))
        {
            animationPlayer.Stop(_orbitAnimationKey);
        }

        if (!animationPlayer.HasRunningAnimation(_orbitStopKey))
        {
            animationPlayer.Play(GetStopAnimation(component, sprite), _orbitStopKey);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var (orbit, sprite) in EntityManager.EntityQuery<OrbitVisualsComponent, SpriteComponent>())
        {
            var angle = new Angle(Math.PI * 2 * orbit.Orbit);
            var vec = angle.RotateVec(new Vector2(orbit.OrbitDistance, 0));

            sprite.Rotation = angle;
            sprite.Offset = vec;
        }
    }

    private void OnAnimationCompleted(EntityUid uid, OrbitVisualsComponent component, AnimationCompletedEvent args)
    {
        if (args.Key == _orbitAnimationKey)
        {
            if(EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? animationPlayer))
                animationPlayer.Play(GetOrbitAnimation(component), _orbitAnimationKey);
        }
    }

    private Animation GetOrbitAnimation(OrbitVisualsComponent component)
    {
        var length = component.OrbitLength;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(OrbitVisualsComponent),
                    Property = nameof(OrbitVisualsComponent.Orbit),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(0.0f, 0f),
                        new AnimationTrackProperty.KeyFrame(1.0f, length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }

    private Animation GetStopAnimation(OrbitVisualsComponent component, SpriteComponent sprite)
    {
        var length = component.OrbitStopLength;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.Reduced(), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, length),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }
}
