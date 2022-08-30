using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private static readonly Animation DefaultDamageAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.3),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Color.Red, 0f),
                    new AnimationTrackProperty.KeyFrame(Color.White, 0.3f)
                }
            }
        }
    };

    private const string DamageAnimationKey = "damage-effect";

    /// <summary>
    /// Gets the red effect animation whenever the server confirms something is hit
    /// </summary>
    public Animation? GetDamageAnimation(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return null;

        // 90% of them are going to be this so why allocate a new class.
        if (sprite.Color.Equals(Color.White))
            return DefaultDamageAnimation;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(0.3),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.Red, 0f),
                        new AnimationTrackProperty.KeyFrame(Color.White, 0.3f)
                    }
                }
            }
        };
    }

    private void OnDamageEffect(DamageEffectEvent ev)
    {
        if (Deleted(ev.Entity))
            return;

        // Need to stop the existing animation first to ensure the sprite color is fixed.
        // Otherwise we might lerp to a red colour instead.
        _animation.Stop(ev.Entity, DamageAnimationKey);
        var animation = GetDamageAnimation(ev.Entity);

        if (animation == null)
            return;

        _animation.Play(ev.Entity, DefaultDamageAnimation, DamageAnimationKey);
    }
}
