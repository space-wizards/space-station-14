using Content.Shared.Weapons;
using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    /// <summary>
    /// It's a little on the long side but given we use multiple colours denoting what happened it makes it easier to register.
    /// </summary>
    private const float DamageAnimationLength = 0.30f;
    private const string DamageAnimationKey = "damage-effect";

    private void InitializeEffect()
    {
        SubscribeLocalEvent<DamageEffectComponent, AnimationCompletedEvent>(OnEffectAnimation);
    }

    private void OnEffectAnimation(EntityUid uid, DamageEffectComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != DamageAnimationKey)
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.Color = component.Color;
        }

        RemCompDeferred<DamageEffectComponent>(uid);
    }

    /// <summary>
    /// Gets the red effect animation whenever the server confirms something is hit
    /// </summary>
    private Animation? GetDamageAnimation(EntityUid uid, Color color, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return null;

        // 90% of them are going to be this so why allocate a new class.
        return new Animation
        {
            Length = TimeSpan.FromSeconds(DamageAnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(color * sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color, DamageAnimationLength)
                    }
                }
            }
        };
    }

    private void OnDamageEffect(DamageEffectEvent ev)
    {
        var color = ev.Color;

        foreach (var ent in ev.Entities)
        {
            if (Deleted(ent))
            {
                continue;
            }

            var player = EnsureComp<AnimationPlayerComponent>(ent);
            player.NetSyncEnabled = false;

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

            var animation = GetDamageAnimation(ent, color, sprite);

            if (animation == null)
                continue;

            var comp = EnsureComp<DamageEffectComponent>(ent);
            comp.NetSyncEnabled = false;
            comp.Color = sprite.Color;
            _animation.Play(player, animation, DamageAnimationKey);
        }
    }
}
