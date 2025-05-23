// This file is intentionally left as a placeholder.
// The radio chime mute system has been moved to be entirely client-side.
// This event is no longer used but is kept for compatibility with existing code references.

using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

/// <summary>
/// This event is no longer used as radio chime muting is now handled entirely client-side.
/// It is kept for compatibility with existing code references.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioChimeMuteEvent : EntityEventArgs
{
    public readonly bool Muted;

    public RadioChimeMuteEvent(bool muted)
    {
        Muted = muted;
    }
}
