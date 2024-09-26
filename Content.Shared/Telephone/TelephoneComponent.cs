using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Telephone;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class TelephoneComponent : Component
{
    /// <summary>
    /// Sets how long the telephone will ring before it automatically hangs up
    /// </summary>
    [DataField]
    public float RingingTimeout = 10;

    /// <summary>
    /// Sets how long the telephone will stay in the hanging up state before return to idle
    /// </summary>
    [DataField]
    public float HangingUpTimeout = 3;

    /// <summary>
    /// Tone played while the phone is ringing
    /// </summary>
    [DataField]
    public SoundSpecifier? RingTone = null;

    /// <summary>
    /// Sets the number of seconds before the next ring tone is played
    /// </summary>
    [DataField]
    public float RingInterval = 2f;

    /// <summary>
    /// The time at which the next tone will be played
    /// </summary>
    [DataField]
    public TimeSpan NextRingToneTime;

    /// <summary>
    /// The volume at which relayed messages are played
    /// </summary>
    [DataField]
    public TelephoneVolume SpeakerVolume;

    /// <summary>
    /// The range at which the telephone can connect to another
    /// </summary>
    [DataField]
    public TelephoneRange TransmissionRange;

    /// <summary>
    /// The range at which the telephone picks up voices
    /// </summary>
    [DataField]
    public float ListeningRange = 2;

    /// <summary>
    /// Linked telephone
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<TelephoneComponent>> LinkedTelephones = new();

    /// <summary>
    /// Defines the current state the telephone is in
    /// </summary>
    [ViewVariables]
    public TelephoneState CurrentState = TelephoneState.Idle;

    /// <summary>
    /// The game tick the current state started
    /// </summary>
    [ViewVariables]
    public TimeSpan StateStartTime;

    /// <summary>
    /// Sets whether the telphone can pick up nearby speech
    /// </summary>
    [ViewVariables]
    public bool Muted = false;
}

#region: Telephone events

/// <summary>
/// Raised when one telephone is attempting to call another
/// </summary>
[ByRefEvent]
public record struct TelephoneCallAttemptEvent(Entity<TelephoneComponent> Source, Entity<TelephoneComponent> Receiver, EntityUid? User)
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised when one telephone is calling another
/// </summary>
[ByRefEvent]
public record struct TelephoneCallEvent(Entity<TelephoneComponent> Source, Entity<TelephoneComponent> Receiver, EntityUid? User);

/// <summary>
/// Raised when a call commences between two telephones
/// </summary>
[ByRefEvent]
public record struct TelephoneCallCommencedEvent(Entity<TelephoneComponent> Source, Entity<TelephoneComponent> Receiver);

/// <summary>
/// Raised when a telephone hangs up
/// </summary>
[ByRefEvent]
public record struct TelephoneCallEndedEvent(Entity<TelephoneComponent> Source);

/// <summary>
/// Raised when a telephone becomes idle
/// </summary>
[ByRefEvent]
public record struct TelephoneCallTerminatedEvent();

[ByRefEvent]
public readonly record struct TelephoneMessageReceivedEvent(string Message, EntityUid MessageSource, Entity<TelephoneComponent> TelephoneSource, MsgChatMessage ChatMsg);

#endregion

/// <summary>
/// Options for tailoring telephone calls
/// </summary>
[Serializable, NetSerializable]
public struct TelephoneCallOptions
{
    public bool ForceConnect;   // The source immediately opens a call with the receiver, potentially interupting a call already in progress 
    public bool ForceJoin;      // The source and can smoothly join a call in progress, ringing the reciever if there is none
    public bool MuteSource;     // Chatter from the source is not transmitted - could be used for eavesdropping when combined with 'ForceJoin'
    public bool MuteReceiver;   // Chatter from the receiver - useful for emergency broadcasts
}

[Serializable, NetSerializable]
public enum TelephoneVisuals : byte
{
    Key
}

[Serializable, NetSerializable]
public enum TelephoneState : byte
{
    Idle,
    Calling,
    Ringing,
    InCall,
    EndingCall
}

[Serializable, NetSerializable]
public enum TelephoneVolume : byte
{
    Whisper,
    Speak
}

[Serializable, NetSerializable]
public enum TelephoneRange : byte
{
    Grid,
    Map,
    Unlimited
}
