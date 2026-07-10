using Content.Shared.Chasm.Components;
using Content.Shared.Chasm.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.Chasm;

/// <summary>
/// Handles the falling animation for entities that fall into an entity with <see cref="ChasmComponent"/>.
/// </summary>
public sealed partial class ChasmFallingVisualsSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<AnimationPlayerComponent> _animationPlayerQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    private const string ChasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChasmFallingVisualsComponent, StartChasmFallingEvent>(OnStartFalling);
        SubscribeLocalEvent<ChasmFallingVisualsComponent, ResetChasmVisualsEvent>(OnResetVisuals);
    }

    private void OnComponentInit(Entity<ChasmFallingComponent> ent, ref ComponentInit args)
    {
        var visuals = EnsureComp<ChasmFallingVisualsComponent>(ent.Owner);
        visuals.AnimationTime = ent.Comp.AnimationTime;
    }

    private void OnStartFalling(Entity<ChasmFallingVisualsComponent> ent, ref StartChasmFallingEvent args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
        entity.Comp.OriginalScale = sprite.Scale;

        if (!_animationPlayerQuery.TryComp(entity, out var player) ||
            _anim.HasRunningAnimation(player, ChasmFallAnimationKey))

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
            return;
        }

        _anim.Play((entity, player), GetFallingAnimation(entity.Comp), ChasmFallAnimationKey);
    }

    private void OnComponentRemove(Entity<ChasmFallingComponent> entity, ref ComponentRemove args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
        {
            return;
        }

        _sprite.SetScale((entity, sprite), entity.Comp.OriginalScale);

        if (!_animationPlayerQuery.TryComp(entity, out var player) ||
            !_anim.HasRunningAnimation(player, ChasmFallAnimationKey))
        {
            return;
        }

        _anim.Stop((entity, player), ChasmFallAnimationKey);
    }

    private static Animation GetFallingAnimation(ChasmFallingComponent component)
    {
        return new Animation
        {
            Length = component.AnimationTime,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, component.AnimationTime.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                },
            },
        };
    }
}
