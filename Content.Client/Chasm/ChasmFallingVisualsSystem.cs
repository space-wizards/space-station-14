using Content.Shared.Chasm;
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

    private readonly string _chasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChasmFallingComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, ChasmFallingComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            TerminatingOrDeleted(uid))
        {
            return;
        }

        component.OriginalScale = sprite.Scale;

        var player = EnsureComp<AnimationPlayerComponent>(uid);
        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            return;

        _anim.Play((uid, player), GetFallingAnimation(component), _chasmFallAnimationKey);
    }

    private void OnComponentRemove(EntityUid uid, ChasmFallingComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var player = EnsureComp<AnimationPlayerComponent>(uid);
        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            _anim.Stop(player, _chasmFallAnimationKey);

        sprite.Scale = component.OriginalScale;
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
