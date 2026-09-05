using System.Numerics;
using Content.Shared.Animation;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client.Animations;

public sealed partial class SineWaveAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly string _sineWaveAnimationKey = "sinWave";

    [SubscribeLocalEvent]
    private void OnStartup(Entity<SineWaveAnimationComponent> ent, ref ComponentStartup args)
    {
        var sprite = Comp<SpriteComponent>(ent);

        ent.Comp.StartOffset = sprite.Offset;
        ent.Comp.StartRotation = sprite.Rotation;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent);

        _animationPlayer.Play((ent, animationPlayer), GetAnimation(ent.Comp, sprite), _sineWaveAnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SineWaveAnimationComponent> ent, ref ComponentShutdown args)
    {
        TryResetFields(ent);

        var animationPlayer = Comp<AnimationPlayerComponent>(ent);
        _animationPlayer.Stop(ent, animationPlayer, _sineWaveAnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(Entity<SineWaveAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != _sineWaveAnimationKey || !args.Finished || !ent.Comp.Repeat)
        {
            TryResetFields(ent);
            return;
        }

        var sprite = Comp<SpriteComponent>(ent);
        _animationPlayer.Play((ent, args.AnimationPlayer), GetAnimation(ent.Comp, sprite), _sineWaveAnimationKey);
    }

    private Animation GetAnimation(SineWaveAnimationComponent sineComp, SpriteComponent sprite)
    {
        if (sineComp is { TotalTimeX.TotalSeconds: 0, XWave: not null })
        {
            sineComp.TotalTimeX = sineComp.XWave.Value.PhaseOffset ??
                                  TimeSpan.FromSeconds(_random.NextDouble(0, sineComp.XWave.Value.Period.TotalSeconds));
        }

        if (sineComp is { TotalTimeY.TotalSeconds: 0, YWave: not null })
        {
            sineComp.TotalTimeY = sineComp.YWave.Value.PhaseOffset ??
                                  TimeSpan.FromSeconds(_random.NextDouble(0, sineComp.YWave.Value.Period.TotalSeconds));
        }

        var rotationKeyFrames = new List<AnimationTrackProperty.KeyFrame>();
        var offsetKeyFrames = new List<AnimationTrackProperty.KeyFrame>();

        rotationKeyFrames.Add(new AnimationTrackProperty.KeyFrame(sprite.Rotation, 0f));
        offsetKeyFrames.Add(new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f));

        var stepValue = sineComp.AnimationLength / sineComp.KeyFrames;

        for (var i = 1; i <= sineComp.KeyFrames; i++)
        {
            var currTime = stepValue * i;

            var offset = new Vector2();
            var rotation = new Angle();

            if (sineComp.XWave is not null)
            {
                var a = (currTime + sineComp.TotalTimeX) / sineComp.XWave.Value.Period;
                offset.X = (float) (sineComp.XWave.Value.Amplitude * Math.Sin(a));

                var angle = new Angle(Math.Atan(Math.Cos(a)));
                rotation += angle;
            }

            if (sineComp.YWave is not null)
            {
                var a = (currTime + sineComp.TotalTimeY) / sineComp.YWave.Value.Period;
                offset.Y = (float) (sineComp.YWave.Value.Amplitude * Math.Cos(a));

                var angle = new Angle(Math.Atan(-Math.Sin(a)));
                rotation += angle;
            }

            rotationKeyFrames.Add(new AnimationTrackProperty.KeyFrame(rotation, (float) stepValue.TotalSeconds));
            offsetKeyFrames.Add(new AnimationTrackProperty.KeyFrame(offset, (float) stepValue.TotalSeconds));
        }

        sineComp.TotalTimeX += sineComp.AnimationLength;
        sineComp.TotalTimeY += sineComp.AnimationLength;

        return new Animation
        {
            Length = sineComp.AnimationLength,
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

    private void TryResetFields(Entity<SineWaveAnimationComponent> ent)
    {
        if (ent.Comp.ResetOffsetOnEnd)
            _sprite.SetOffset(ent.Owner, ent.Comp.StartOffset);

        if (ent.Comp.ResetRotationOnEnd)
            _sprite.SetRotation(ent.Owner, ent.Comp.StartRotation);
    }
}
