using System.Numerics;
using Content.Shared._DarkAscent.Trigger.Animate;
using Content.Shared.StepTrigger.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._DarkAscent.Trigger.Animate;

public sealed class AnimateOnStepTriggerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string AnimationKey = "stepAnimate";

    /// <summary>
    /// Tracks the last sound played for the system itself. Moved here from the component so it affects all entities.
    /// </summary>
    private TimeSpan _globalSoundLastPlayed;

    public override void Initialize()
    {
        SubscribeNetworkEvent<AnimateOnStepTriggerEvent>(OnStep);
    }

    private void OnStep(AnimateOnStepTriggerEvent ev)
    {
        if (!TryGetEntity(ev.Uid, out var uid))
            return;

        if (!TryComp(uid, out AnimateOnStepTriggerComponent? animComp))
            return;

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        if (!TryComp(uid, out AnimationPlayerComponent? player))
            return;

        PlayAnimation(uid.Value, player, sprite);

        // We don't want grass sound spam.
        var curTime = _timing.CurTime;
        if (curTime < _globalSoundLastPlayed + TimeSpan.FromSeconds(animComp.SoundCooldown))
            return;

        _audio.PlayPvs(animComp.Sound, uid.Value);
        _globalSoundLastPlayed = curTime;

    }

    // I made this for grass, optionally parameterize this into the component for generic use.
    private void PlayAnimation(EntityUid uid, AnimationPlayerComponent player, SpriteComponent sprite)
    {
        // Don't overlap animations or everything crashes.
        _animationPlayer.Stop(uid, player, AnimationKey);


        var offset = new Vector2(_random.NextFloat(-0.05f, 0.05f), _random.NextFloat(-0.05f, 0.05f));
        var start = sprite.Offset;

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(0.3),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(start, 0f),
                        new AnimationTrackProperty.KeyFrame(start + offset, 0.1f),
                        new AnimationTrackProperty.KeyFrame(start - offset, 0.2f),
                        new AnimationTrackProperty.KeyFrame(start, 0.3f),
                    }
                }
            }
        };

        _animationPlayer.Play((uid, player), anim, AnimationKey);
    }
}
