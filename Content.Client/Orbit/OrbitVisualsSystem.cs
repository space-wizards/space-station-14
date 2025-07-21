using System.Numerics;
using Content.Shared.Follower.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Orbit;

public sealed class OrbitVisualsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AnimationPlayerSystem _animations = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly string _orbitStopKey = "orbiting_stop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<OrbitVisualsComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, OrbitVisualsComponent component, ComponentInit args)
    {
        _robustRandom.SetSeed((int)_timing.CurTime.TotalMilliseconds);
        component.OrbitDistance =
            _robustRandom.NextFloat(0.75f * component.OrbitDistance, 1.25f * component.OrbitDistance);

        component.OrbitLength = _robustRandom.NextFloat(0.5f * component.OrbitLength, 1.5f * component.OrbitLength);

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = Direction.South;
        }

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);
        if (_animations.HasRunningAnimation(uid, animationPlayer, _orbitStopKey))
        {
            _animations.Stop((uid, animationPlayer), _orbitStopKey);
        }
    }

    private void OnComponentRemove(EntityUid uid, OrbitVisualsComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.EnableDirectionOverride = false;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(uid);
        if (!_animations.HasRunningAnimation(uid, animationPlayer, _orbitStopKey))
        {
            _animations.Play((uid, animationPlayer), GetStopAnimation(component, sprite), _orbitStopKey);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityManager.EntityQueryEnumerator<OrbitVisualsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var orbit, out var sprite))
        {
            var progress = (float)(_timing.CurTime.TotalSeconds / orbit.OrbitLength) % 1;
            var angle = new Angle(Math.PI * 2 * progress);
            var vec = angle.RotateVec(new Vector2(orbit.OrbitDistance, 0));

            _sprite.SetRotation((uid, sprite), angle);
            _sprite.SetOffset((uid, sprite), vec);
        }
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
