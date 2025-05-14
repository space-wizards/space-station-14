using Robust.Shared.Serialization;

namespace Content.Shared.Radio;

/// <summary>
/// Event sent from client to server to inform the server about the client's radio chime mute preference.
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
