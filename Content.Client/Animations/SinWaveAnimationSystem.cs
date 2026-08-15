using System.Numerics;
using Content.Shared.Animation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Animations;

public sealed partial class SinWaveAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly string _sinWaveAnimationKey = "sinWave";

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SinWaveAnimationComponent> ent, ref ComponentStartup args)
    {
        var sprite = Comp<SpriteComponent>(ent);

        ent.Comp.StartOffset = sprite.Offset;
        ent.Comp.StartRotation = sprite.Rotation;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent);

        _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _sinWaveAnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SinWaveAnimationComponent> ent, ref ComponentShutdown args)
    {
        TryResetFields(ent);

        var animationPlayer = Comp<AnimationPlayerComponent>(ent);
        _animationPlayer.Stop(ent, animationPlayer, _sinWaveAnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(Entity<SinWaveAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != _sinWaveAnimationKey || !args.Finished || !ent.Comp.Repeat)
        {
            TryResetFields(ent);
            return;
        }

        var sprite = Comp<SpriteComponent>(ent);
        _animationPlayer.Play((ent, args.AnimationPlayer), GetAnimation(ent.Comp, sprite), _sinWaveAnimationKey);
    }

    private Animation GetAnimation(SinWaveAnimationComponent sinComp, SpriteComponent sprite)
    {
        if (sinComp.LastTime == 0 && sinComp.XWave != null && sinComp.XWave.Value.PhaseOffset == null)
            sinComp.LastTime = _random.NextFloat(0, 2f*(float)Math.PI / sinComp.XWave.Value.Frequency);

        var rotationKeyFrames = new List<AnimationTrackProperty.KeyFrame>();
        var offsetKeyFrames = new List<AnimationTrackProperty.KeyFrame>();

        rotationKeyFrames.Add(new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0f));
        offsetKeyFrames.Add(new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f));

        var stepValue = sinComp.AnimationLength / sinComp.KeyFrames;

        for (var i = 1; i <= sinComp.KeyFrames; i++)
        {
            var currTime = stepValue * i;

            var offset = new Vector2();
            var rotation = new Angle();

            if (sinComp.XWave != null)
            {
                var a = sinComp.XWave.Value.Frequency * (currTime + sinComp.LastTime);
                offset.X = (float) (sinComp.XWave.Value.Amplitude * Math.Sin(a));

                var angle = new Angle(Math.Atan(Math.Cos(a)));
                rotation += angle;
            }

            if (sinComp.YWave != null)
            {
                var a = sinComp.YWave.Value.Frequency * (currTime + sinComp.LastTime);
                offset.Y = (float) (sinComp.YWave.Value.Amplitude * Math.Cos(a));

                // TODO: I think this is slightly off
                var angle = new Angle(Math.Atan(-Math.Sin(a)));
                rotation += angle;
            }

            rotationKeyFrames.Add(new AnimationTrackProperty.KeyFrame(rotation, stepValue));
            offsetKeyFrames.Add(new AnimationTrackProperty.KeyFrame(offset, stepValue));
        }

        sinComp.LastTime += sinComp.AnimationLength;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(sinComp.AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames = rotationKeyFrames,
                },

                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames = offsetKeyFrames,
                },
            },
        };
    }

    private void TryResetFields(Entity<SinWaveAnimationComponent> ent)
    {
        if (ent.Comp.ResetOffsetOnEnd)
            _sprite.SetOffset(ent.Owner, ent.Comp.StartOffset);

        if (ent.Comp.ResetRotationOnEnd)
            _sprite.SetRotation(ent.Owner, ent.Comp.StartRotation);
    }
}
