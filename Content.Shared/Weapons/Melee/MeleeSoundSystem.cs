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
    /// Appends all the SoundSpecifiers from one MeleeSoundComponent to another
    /// </summary>
    public void appendSoundsFrom(MeleeSoundComponent srcComp, MeleeSoundComponent dstComp)
    {

    }
    /// <summary>
    /// uh uh.
    /// This should remove it's sounds from the destination component
    /// </summary>
    public void removeSoundsOf(MeleeSoundComponent ){}

    public void PlayHitSound(EntityUid target, EntityUid? user, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound, SoundSpecifier? noDamageSound)
    {
        var playedSound = false;

        if (Deleted(target))
            return;

        // hitting can obv destroy an entity so we play at coords and not following them
        var coords = Transform(target).Coordinates;
        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
        {
            if (type == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPredicted(damageSoundComp.NoDamageSound, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPredicted(damageSoundType, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPredicted(damageSoundGroup, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPredicted(hitSoundOverride, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPredicted(hitSound, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (noDamageSound != null)
            {
                Audio.PlayPredicted(noDamageSound, coords, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (type)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Radiation":
                case "Cold":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPredicted(new SoundCollectionSpecifier("WeakHit"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPredicted(new SoundCollectionSpecifier("MetalThud"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

}
