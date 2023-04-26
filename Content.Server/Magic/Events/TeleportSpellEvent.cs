using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Server.Magic.Events;

public sealed class TeleportSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    [DataField("speech")]
    public string? Speech { get; }

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("blinkVolume")]
    public float BlinkVolume = 5f;
}
