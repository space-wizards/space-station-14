using Content.Shared.Throwing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client.Throwing;

/// <summary>
///     Handles animating thrown items.
/// </summary>
public sealed partial class ThrownItemVisualizerSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string AnimationKey = "thrown-item";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrownItemComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);
        SubscribeLocalEvent<ThrownItemComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAutoHandleState(Entity<ThrownItemComponent> thrown, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(thrown, out var sprite) || !thrown.Comp.Animate)
            return;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(thrown);

        if (_anim.HasRunningAnimation(thrown, animationPlayer, AnimationKey))
            return;

        var anim = GetAnimation((thrown.Owner, thrown.Comp, sprite));
        if (anim == null)
            return;

        thrown.Comp.OriginalScale = sprite.Scale;
        _anim.Play((thrown, animationPlayer), anim, AnimationKey);
    }

    private void OnShutdown(Entity<ThrownItemComponent> thrown, ref ComponentShutdown args)
    {
        if (!_anim.HasRunningAnimation(thrown, AnimationKey))
            return;

        if (TryComp<SpriteComponent>(thrown, out var sprite) && thrown.Comp.OriginalScale != null)
            _sprite.SetScale((thrown, sprite), thrown.Comp.OriginalScale.Value);

        _anim.Stop(thrown.Owner, AnimationKey);
    }

    private static Animation? GetAnimation(Entity<ThrownItemComponent, SpriteComponent> ent)
    {
        if (ent.Comp1.LandTime - ent.Comp1.ThrownTime is not { } length)
            return null;

        if (length <= TimeSpan.Zero)
            return null;

        var scale = ent.Comp2.Scale;
        var lenFloat = (float)length.TotalSeconds;

        // TODO use like actual easings here
        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(scale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(scale * 1.4f, lenFloat * 0.25f),
                        new AnimationTrackProperty.KeyFrame(scale, lenFloat * 0.75f)
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };
    }
}
