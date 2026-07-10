using Content.Shared.Chasm;
using Content.Shared.Chasm.Components;
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
        SubscribeLocalEvent<ChasmFallingVisualsComponent, StartedFallingIntoChasmEvent>(OnStartFalling);
        SubscribeLocalEvent<ChasmFallingVisualsComponent, ResetChasmVisualsEvent>(OnResetVisuals);
    }

    private void OnComponentInit(Entity<ChasmFallingComponent> ent, ref ComponentInit args)
    {
        var visuals = EnsureComp<ChasmFallingVisualsComponent>(ent.Owner);
        visuals.AnimationTime = ent.Comp.AnimationTime;
    }

    private void OnStartFalling(Entity<ChasmFallingVisualsComponent> ent, ref StartedFallingIntoChasmEvent args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        ent.Comp.OriginalScale = sprite.Scale;
        if (!_animationPlayerQuery.TryComp(ent, out var player)
            || _anim.HasRunningAnimation(player, ChasmFallAnimationKey))
            return;

        _anim.Play((ent.Owner, player), GetFallingAnimation(ent.Comp), ChasmFallAnimationKey);
    }

    private void OnResetVisuals(Entity<ChasmFallingVisualsComponent> entity, ref ResetChasmVisualsEvent args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
            return;

        if (entity.Comp.OriginalScale != null)
            _sprite.SetScale((entity, sprite), entity.Comp.OriginalScale.Value);

        if (!_animationPlayerQuery.TryComp(entity, out var player) ||
            !_anim.HasRunningAnimation(player, ChasmFallAnimationKey))
            return;

        _anim.Stop((entity, player), ChasmFallAnimationKey);
    }

    private static Animation GetFallingAnimation(ChasmFallingVisualsComponent component)
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
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale!, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, component.AnimationTime.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                },
            },
        };
    }
}
