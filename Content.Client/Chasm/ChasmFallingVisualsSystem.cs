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

        SubscribeLocalEvent<ChasmFallingComponent, ComponentStartup>(OnComponentInit);
        SubscribeLocalEvent<ChasmFallingComponent, ChasmFallEffectsEvent>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, ChasmFallingComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite)
            || TerminatingOrDeleted(uid))
        {
            return;
        }

        component.OriginalScale = sprite.Scale;

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            return;

        _anim.Play((uid, player), GetFallingAnimation(component), _chasmFallAnimationKey);
    }

    private void OnComponentRemove(EntityUid uid, ChasmFallingComponent component, ref ChasmFallEffectsEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetScale((uid, sprite), component.OriginalScale);

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            _anim.Stop((uid, player), _chasmFallAnimationKey);
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
