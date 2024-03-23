using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class SmokeSpellEvent : InstantActionEvent, ISpeakSpell
{
    /// <summary>
    /// The range this spell opens doors in
    /// 4f is the default
    /// </summary>
    [DataField("range")]
    public float Range = 4f;

    [DataField("knockSound")]
    public SoundSpecifier SmokeSound = new SoundPathSpecifier("/Audio/Magic/knock.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("knockVolume")]
    public float SmokeVolume = 5f;

    [DataField("speech")]
    public string? Speech { get; private set; }
}
