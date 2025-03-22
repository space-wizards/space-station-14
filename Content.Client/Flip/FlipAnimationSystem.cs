using System.Numerics;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Content.Shared.Flip;

namespace Content.Client.Flip;

public sealed class FlipAnimationSystem : SharedFlipAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StartFlipEvent>(OnStartFlip);
        SubscribeNetworkEvent<StopFlipEvent>(OnStopFlip);
    }

    private void OnStartFlip(StartFlipEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Entity);
        if (!TryComp<FlipAnimationComponent>(uid, out var comp))
            return;

        if (_animation.HasRunningAnimation(uid, comp.KeyName))
            return;

        PlayFlipAnimation((uid, comp));
    }

    private void OnStopFlip(StopFlipEvent msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Entity);

        if (!TryComp<FlipAnimationComponent>(GetEntity(msg.Entity), out var comp))
            return;

        if (!_animation.HasRunningAnimation(uid, comp.KeyName))
            return;

        _animation.Stop(uid, comp.KeyName);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Offset = new Vector2();
        sprite.Rotation = Angle.FromDegrees(0);
    }

    private void PlayFlipAnimation(Entity<FlipAnimationComponent> ent)
    {
        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(ent.Comp.AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Angle(0), 0),
                        new AnimationTrackProperty.KeyFrame(new Angle(Math.PI), ent.Comp.AnimationLength / 2), // needed for proper interpolation
                        new AnimationTrackProperty.KeyFrame(new Angle(2 * Math.PI), ent.Comp.AnimationLength / 2),
                    }
                }
            }
        };

        _animation.Play(ent.Owner, anim, ent.Comp.KeyName);
    }
}
