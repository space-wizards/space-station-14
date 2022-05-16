using Content.Shared.Sound;

namespace Content.Server.Weapon.Melee.Components;

/// <summary>
/// Plays the specified sound upon receiving damage of the specified type.
/// </summary>
[RegisterComponent]
public sealed class MeleeSoundComponent : Component
{
    /// <summary>
    /// Specified sounds to apply when the entity takes damage. Will fallback to defaults if none specified.
    /// </summary>
    [DataField("sounds", required: true)]
    public Dictionary<string, SoundSpecifier> Sounds = new();

    /// <summary>
    /// Sound that plays if no damage is done.
    /// </summary>
    [DataField("noDamageSound")] public SoundSpecifier? NoDamageSound = null;
}
