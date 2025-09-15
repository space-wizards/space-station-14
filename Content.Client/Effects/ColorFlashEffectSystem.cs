using Content.Shared.Effects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Effects;

public sealed class ColorFlashEffectSystem : SharedColorFlashEffectSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    /// It's a little on the long side but given we use multiple colours denoting what happened it makes it easier to register.
    /// </summary>
    private const float AnimationLength = 0.30f;
    private const string AnimationKey = "color-flash-effect";
    private ValueList<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ColorFlashEffectEvent>(OnColorFlashEffect);
        SubscribeLocalEvent<ColorFlashEffectComponent, AnimationCompletedEvent>(OnEffectAnimationCompleted);
    }

    public override void RaiseEffect(Color color, List<EntityUid> entities, Filter filter)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        OnColorFlashEffect(new ColorFlashEffectEvent(color, GetNetEntityList(entities)));
    }

    private void OnEffectAnimationCompleted(EntityUid uid, ColorFlashEffectComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            _sprite.SetColor((uid, sprite), component.Color);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ColorFlashEffectComponent>();
        _toRemove.Clear();

        // Can't use deferred removal on animation completion or it will cause issues.
        while (query.MoveNext(out var uid, out _))
        {
            if (_animation.HasRunningAnimation(uid, AnimationKey))
                continue;

            _toRemove.Add(uid);
        }

        foreach (var ent in _toRemove)
        {
            RemComp<ColorFlashEffectComponent>(ent);
        }
    }

    private Animation? GetDamageAnimation(EntityUid uid, Color color, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return null;

        // 90% of them are going to be this so why allocate a new class.
        return new Animation
        {
            Length = TimeSpan.FromSeconds(AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color, AnimationLength)
                    }
                }
            }
        };
    }

    private void OnColorFlashEffect(ColorFlashEffectEvent ev)
    {
        var color = ev.Color;

        foreach (var nent in ev.Entities)
        {
            var ent = GetEntity(nent);

            if (Deleted(ent) || !TryComp(ent, out SpriteComponent? sprite))
            {
                continue;
            }

            if (!TryComp(ent, out ColorFlashEffectComponent? comp))
            {
#if DEBUG
                DebugTools.Assert(!_animation.HasRunningAnimation(ent, AnimationKey));
#endif
            }

            _animation.Stop(ent, AnimationKey);
            var animation = GetDamageAnimation(ent, color, sprite);

            if (animation == null)
            {
                continue;
            }

            var targetEv = new GetFlashEffectTargetEvent(ent);
            RaiseLocalEvent(ent, ref targetEv);
            ent = targetEv.Target;

            EnsureComp<ColorFlashEffectComponent>(ent, out comp);
            comp.NetSyncEnabled = false;
            comp.Color = sprite.Color;

            _animation.Play(ent, animation, AnimationKey);
        }
    }
}

/// <summary>
/// Raised on an entity to change the target for a color flash effect.
/// </summary>
[ByRefEvent]
public record struct GetFlashEffectTargetEvent(EntityUid Target);
