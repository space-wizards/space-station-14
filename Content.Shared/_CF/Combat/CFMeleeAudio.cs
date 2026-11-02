using Robust.Shared.Audio;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Default hit sounds for CF weapons that do not set <c>soundHit</c> on <c>MeleeWeapon</c>.
/// Swing sounds stay on that component (<c>soundSwing</c>, fist whoosh unless a weapon overrides it).
/// </summary>
public static class CFMeleeAudio
{
    private static readonly SoundSpecifier Slash = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");
    private static readonly SoundSpecifier Blunt = new SoundPathSpecifier("/Audio/Weapons/smash.ogg");

    public static SoundSpecifier HitSound(string cfDamageType)
    {
        return cfDamageType switch
        {
            "bludgeoning" => Blunt,
            _ => Slash,
        };
    }
}
