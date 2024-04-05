using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class SmokeSpellEvent : InstantActionEvent, ISpeakSpell
{


    [DataField("knockSound")]
    public SoundSpecifier SmokeSound = new SoundPathSpecifier("/Audio/Effects/smoke.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("knockVolume")]
    public float SmokeVolume = 5f;

    [DataField("speech")]
    public string? Speech { get; private set; }
}
