using Content.Shared.Chasm.Components;
using Content.Shared.Chasm.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Chasm;

/// <summary>
///     Handles the falling animation for entities that fall into a chasm.
/// </summary>
public sealed class ChasmFallingVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly string _chasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, StartChasmFallingEvent>(OnStartFalling);
        SubscribeLocalEvent<ChasmFallingComponent, ResetChasmVisualsEvent>(OnResetVisuals);
    }

    private void OnStartFalling(Entity<ChasmFallingComponent> ent, ref StartChasmFallingEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ent.Comp.OriginalScale = sprite.Scale;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent.Owner);

        if (_anim.HasRunningAnimation(animationPlayer, _chasmFallAnimationKey))
            return;

        _anim.Play((ent.Owner, animationPlayer), GetFallingAnimation(ent.Comp), _chasmFallAnimationKey);
    }

    private void OnResetVisuals(Entity<ChasmFallingComponent> ent, ref ResetChasmVisualsEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        _sprite.SetScale((ent.Owner, sprite), ent.Comp.OriginalScale);

        if (!TryComp<AnimationPlayerComponent>(ent.Owner, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            _anim.Stop((ent.Owner, player), _chasmFallAnimationKey);
    }

    private Animation GetFallingAnimation(ChasmFallingComponent component)
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
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, length.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic
                }
            }
        };
    }
}
