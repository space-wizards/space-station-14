using Content.Shared.Weapons;
using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private static readonly Animation DefaultDamageAnimation = new()
    {
        Length = TimeSpan.FromSeconds(DamageAnimationLength),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Color.Red, 0f),
                    new AnimationTrackProperty.KeyFrame(Color.White, DamageAnimationLength)
                }
            }
        }
    };

    private const float DamageAnimationLength = 0.15f;
    private const string DamageAnimationKey = "damage-effect";

    private void InitializeEffect()
    {
        SubscribeLocalEvent<DamageEffectComponent, AnimationCompletedEvent>(OnEffectAnimation);
    }

    private void OnEffectAnimation(EntityUid uid, DamageEffectComponent component, AnimationCompletedEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.Color = component.Color;
        }

        RemCompDeferred<DamageEffectComponent>(uid);
    }

    /// <summary>
    /// Gets the red effect animation whenever the server confirms something is hit
    /// </summary>
    public Animation? GetDamageAnimation(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return null;

        // 90% of them are going to be this so why allocate a new class.
        if (sprite.Color.Equals(Color.White))
            return DefaultDamageAnimation;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(DamageAnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.Red * sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color, DamageAnimationLength)
                    }
                }
            }
        };
    }

    private void OnDamageEffect(DamageEffectEvent ev)
    {
        foreach (var ent in ev.Entities)
        {
            if (Deleted(ent))
            {
                continue;
            }

            var player = EnsureComp<AnimationPlayerComponent>(ent);

            // Need to stop the existing animation first to ensure the sprite color is fixed.
            // Otherwise we might lerp to a red colour instead.
            if (_animation.HasRunningAnimation(ent, player, DamageAnimationKey))
            {
                _animation.Stop(ent, player, DamageAnimationKey);
            }

            if (!TryComp<SpriteComponent>(ent, out var sprite))
            {
                continue;
            }

            if (TryComp<DamageEffectComponent>(ent, out var effect))
            {
                sprite.Color = effect.Color;
            }

            var animation = GetDamageAnimation(ent, sprite);

            if (animation == null)
                continue;

            var comp = EnsureComp<DamageEffectComponent>(ent);
            comp.NetSyncEnabled = false;
            comp.Color = sprite.Color;
            _animation.Play(player, DefaultDamageAnimation, DamageAnimationKey);
        }
    }
}
