using Content.Shared.Weapons.Melee;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    /// <summary>
    /// Plays the red effect whenever the server confirms something is hit
    /// </summary>
    public static readonly Animation DamageAnimation = new()
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

    private void OnDamageEffect(DamageEffectEvent ev)
    {
        if (Deleted(ev.Entity))
            return;

        _animation.Stop(ev.Entity, DamageAnimationKey);
        _animation.Play(ev.Entity, DamageAnimation, DamageAnimationKey);
    }
}
