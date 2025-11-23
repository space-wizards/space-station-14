using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Disposal.Unit;

/// <inheritdoc/>
public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private const string AnimationKey = "disposal_unit_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<DisposalUnitComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

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
                    LayerKey = DisposalUnitVisualLayers.Base,
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

    private void OnHandleState(EntityUid uid, DisposalUnitComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI((uid, component));
    }

    private void OnAppearanceChange(Entity<DisposalUnitComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(ent, args.Sprite, args.Component);
    }

    /// <summary>
    /// Updates the animation of a disposal unit.
    /// </summary>
    /// <param name="ent">The disposal unit.</param>
    /// <param name="sprite">The disposal unit's sprite.</param>
    /// <param name="appearance">The disposal unit's appearance.</param>
    private void UpdateState(Entity<DisposalUnitComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearanceSystem.TryGetData<bool>(ent, DisposalUnitVisuals.IsFlushing, out var isFlushing, appearance))
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

