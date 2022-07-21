using Content.Shared.Actions;
using Content.Shared.Sound;

namespace Content.Server.Magic.Events;

public sealed class KnockSpellEvent : InstantActionEvent
{
    /// <summary>
    /// The range this spell opens doors in
    /// 4f is the default
    /// </summary>
    [DataField("range")]
    public float Range = 4f;

    [DataField("knockSound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Magic/knock.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("knockVolume")]
    public float KnockVolume = 5f;
}
