using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

// TODO: Can probably just be an entity or something
public sealed partial class TeleportSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField("speech")]
    public string? Speech { get; private set; }

    // TODO: Move to magic component
    // TODO: Maybe not since sound specifier is a thing
    // Keep here to remind what the volume was set as
    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("blinkVolume")]
    public float BlinkVolume = 5f;
}
