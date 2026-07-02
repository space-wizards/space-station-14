using Content.Shared.Chasm.Components;
using Content.Shared.Chasm.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Chasm;

/// <summary>
///     Handles the falling animation for entities that fall into a chasm.
/// </summary>
public sealed partial class ChasmFallingVisualsSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly string _chasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnStartup);
        SubscribeLocalEvent<ChasmFallingVisualsComponent, StartChasmFallingEvent>(OnStartFalling);
        SubscribeLocalEvent<ChasmFallingVisualsComponent, ResetChasmVisualsEvent>(OnResetVisuals);
    }

    private void OnStartup(Entity<ChasmFallingComponent> ent, ref ComponentInit args)
    {
        var visuals = EnsureComp<ChasmFallingVisualsComponent>(ent.Owner);
        visuals.AnimationTime = ent.Comp.AnimationTime;
    }

    private void OnStartFalling(Entity<ChasmFallingVisualsComponent> ent, ref StartChasmFallingEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ent.Comp.OriginalScale ??= sprite.Scale;
        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent.Owner);

        if (_anim.HasRunningAnimation(animationPlayer, _chasmFallAnimationKey))
            return;

        _anim.Play((ent.Owner, animationPlayer), GetFallingAnimation(ent.Comp), _chasmFallAnimationKey);
    }

    private void OnResetVisuals(Entity<ChasmFallingVisualsComponent> ent, ref ResetChasmVisualsEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (ent.Comp.OriginalScale != null)
            _sprite.SetScale((ent.Owner, sprite), ent.Comp.OriginalScale.Value);

        if (!TryComp<AnimationPlayerComponent>(ent.Owner, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            _anim.Stop((ent.Owner, player), _chasmFallAnimationKey);
    }

    private Animation GetFallingAnimation(ChasmFallingVisualsComponent component)
    {
        var length = component.AnimationTime;

        return new Animation()
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        // Supress null because the method is only used in a place that 100% assigns the scale
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale!, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, length.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic
                }
            }
        };
    }
}
