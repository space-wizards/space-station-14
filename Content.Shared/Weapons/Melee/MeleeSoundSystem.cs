using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This handles <see cref="MeleeSoundComponent"/>
/// </summary>
public sealed class MeleeSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem Audio = default!;

    public const float DamagePitchVariation = 0.05f;

    /// <summary>
    /// Plays the SwingSound from a weapon component
    /// for immediate feedback, misses and such
    /// (Swinging a weapon goes "whoosh" whether it hits or not)
    /// </summary>
    public void PlaySwingSound(EntityUid userUid, EntityUid weaponUid, MeleeWeaponComponent weaponComponent)
    {
        Audio.PlayPredicted(weaponComponent.SwingSound, weaponUid, userUid);
    }

    /// <summary>
    /// Takes a "damageType" string as an argument and uses it to
    /// search one of the various Dictionaries in the MeleeSoundComponent
    /// for a sound to play, and falls back if that fails
    /// </summary>
    /// <param name="damageType"> Serves as a lookup key for a hit sound </param>
    /// <param name="hitSoundOverride"> A sound can be supplied by the <see cref="MeleeHitEvent"/> itself to override everything else </param>
    public void PlayHitSound(EntityUid targetUid, EntityUid? userUid, string? damageType, SoundSpecifier? hitSoundOverride, MeleeWeaponComponent weaponComponent)
    {
        SoundSpecifier? hitSound      = weaponComponent.HitSound;
        SoundSpecifier? noDamageSound = weaponComponent.NoDamageSound;

        var playedSound = false;

        if (Deleted(targetUid))
            return;

        // hitting can obv destroy an entity so we play at coords and not following them
        var coords = Transform(targetUid).Coordinates;
        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(targetUid, out var damageSoundComp))
        {
            if (damageType == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPredicted(damageSoundComp.NoDamageSound, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (damageType != null && damageSoundComp.SoundTypes?.TryGetValue(damageType, out var damageSoundType) == true)
            {
                Audio.PlayPredicted(damageSoundType, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (damageType != null && damageSoundComp.SoundGroups?.TryGetValue(damageType, out var damageSoundGroup) == true)
            {
                Audio.PlayPredicted(damageSoundGroup, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPredicted(hitSoundOverride, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPredicted(hitSound, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (noDamageSound != null)
            {
                Audio.PlayPredicted(noDamageSound, coords, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (damageType)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Radiation":
                case "Cold":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/welder.ogg"), targetUid, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPredicted(new SoundCollectionSpecifier("WeakHit"), targetUid, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPredicted(new SoundCollectionSpecifier("MetalThud"), targetUid, userUid, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

}
