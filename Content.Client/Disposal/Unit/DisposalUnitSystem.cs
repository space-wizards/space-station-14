using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Disposal.Unit;

/// <inheritdoc/>
public sealed partial class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    private const string AnimationKey = "disposal_unit_animation";

    protected override void OnComponentInit(Entity<DisposalUnitComponent> ent, ref ComponentInit args)
    {
        base.OnComponentInit(ent, ref args);

        // Create and store flushing animation.
        var anim = new Animation
        {
            Length = ent.Comp.FlushDelay,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = DisposalUnitVisualLayers.OverlayFlushing,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.FlushingState, 0f) },
                },
            }
        };

        // Try to add flushing sound
        if (ent.Comp.FlushSound != null)
        {
            anim.AnimationTracks.Add(
                new AnimationTrackPlaySound
                {
                    KeyFrames = { new AnimationTrackPlaySound.KeyFrame(_audioSystem.ResolveSound(ent.Comp.FlushSound), 0) }
                }
            );
        }

        ent.Comp.FlushingAnimation = anim;
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<DisposalUnitComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<DisposalUnitComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<bool>(DisposalUnitVisuals.IsFlushing, out var isFlushing))
            return;

        // This is a transient state so not too worried about replaying in range.
        if (isFlushing)
        {
            if (!_animationSystem.HasRunningAnimation(ent, AnimationKey))
            {
                _animationSystem.Play(ent, (Animation)ent.Comp.FlushingAnimation, AnimationKey);
            }

            return;
        }

        _animationSystem.Stop(ent.Owner, AnimationKey);
    }
}

