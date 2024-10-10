using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class TelephoneComponent : Component
{
    /// <summary>
    /// Sets how long the telephone will ring before it automatically hangs up
    /// </summary>
    [DataField]
    public float RingingTimeout = 30;

    /// <summary>
    /// Sets how long the telephone will stay in the hanging up state before return to idle
    /// </summary>
    [DataField]
    public float HangingUpTimeout = 2;

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
    public TelephoneVolume SpeakerVolume = TelephoneVolume.Whisper;

    /// <summary>
    /// The range at which the telephone can connect to another
    /// </summary>
    [DataField]
    public TelephoneRange TransmissionRange = TelephoneRange.Grid;

    /// <summary>
    /// The range at which the telephone picks up voices
    /// </summary>
    [DataField]
    public float ListeningRange = 2;

    /// <summary>
    /// Specifies whether this telephone require power to fucntion
    /// </summary>
    [DataField]
    public bool RequiresPower = true;

    /// <summary>
    /// This telephone does not appear on public telephone directories
    /// </summary>
    [DataField]
    public bool UnlistedNumber = false;

    /// <summary>
    /// Telephone number for this device
    /// </summary>
    /// <remarks>
    /// For future use - a system for generating and handling telephone numbers has not been implemented yet
    /// </remarks>
    [ViewVariables]
    public int TelephoneNumber = -1;

    /// <summary>
    /// Linked telephone
    /// </summary>
    [ViewVariables]
    public HashSet<Entity<TelephoneComponent>> LinkedTelephones = new();

    /// <summary>
    /// Defines the current state the telephone is in
    /// </summary>
    [ViewVariables, AutoNetworkedField]
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

    /// <summary>
    /// The last person to call this telephone
    /// </summary>
    [ViewVariables]
    public EntityUid? LastCaller;
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
/// Raised when a telephone's state changes
/// </summary>
[ByRefEvent]
public record struct TelephoneStateChangeEvent(TelephoneState OldState, TelephoneState NewState);

/// <summary>
/// Raised when communication between one telephone and another begins
/// </summary>
[ByRefEvent]
public record struct TelephoneCallCommencedEvent(Entity<TelephoneComponent> Receiver);

/// <summary>
/// Raised when a telephone hangs up
/// </summary>
[ByRefEvent]
public record struct TelephoneCallEndedEvent();

/// <summary>
/// Raised when a chat message is sent by a telephone to another
/// </summary>
[ByRefEvent]
public readonly record struct TelephoneMessageSentEvent(string Message, MsgChatMessage ChatMsg, EntityUid MessageSource);

/// <summary>
/// Raised when a chat message is received by a telephone from another
/// </summary>
[ByRefEvent]
public readonly record struct TelephoneMessageReceivedEvent(string Message, MsgChatMessage ChatMsg, EntityUid MessageSource, Entity<TelephoneComponent> TelephoneSource);

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
    Grid,       // Can call telephones on the same grid 
    Map,        // Can call telephones on the same map 
    Long,       // Can only call telephones that 1) are on a different maps and 2) are also long range
    Unlimited   // Can call any telephone
}
