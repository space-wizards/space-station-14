using Content.Shared.Chat;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class TelephoneComponent : Component
{
    /// <summary>
    /// Sets how long the telephone will ring before it automatically hangs up
    /// </summary>
    [DataField]
    public TimeSpan RingingTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Sets how long the telephone can remain idle in-call before it automatically hangs up
    /// </summary>
    [DataField]
    public TimeSpan IdlingTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Sets how long the telephone will stay in the hanging up state before return to idle
    /// </summary>
    [DataField]
    public TimeSpan HangingUpTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Tone played while the phone is ringing
    /// </summary>
    [DataField]
    public SoundSpecifier? RingTone = null;

    /// <summary>
    /// Sets the number of seconds before the next ring tone is played
    /// </summary>
    [DataField]
    public TimeSpan RingInterval = TimeSpan.FromSeconds(2);

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
    /// The maximum range at which the telephone initiate a call with another
    /// </summary>
    [DataField]
    public TelephoneRange TransmissionRange = TelephoneRange.Grid;

    /// <summary>
    /// This telephone will ignore devices that share the same grid as it
    /// </summary>
    /// <remarks>
    /// This bool will be ignored if the <see cref="TransmissionRange"/> is
    /// set to <see cref="TelephoneRange.Grid"/>
    /// </remarks>
    [DataField]
    public bool IgnoreTelephonesOnSameGrid = false;

    /// <summary>
    /// The telephone can only connect with other telephones which have a
    /// <see cref="TransmissionRange"/> present in this list
    /// </summary>
    [DataField]
    public List<TelephoneRange> CompatibleRanges = new List<TelephoneRange>() { TelephoneRange.Grid };

    /// <summary>
    /// The range at which the telephone picks up voices
    /// </summary>
    [DataField]
    public float ListeningRange = 2;

    /// <summary>
    /// This telephone should not appear on public telephone directories
    /// </summary>
    [DataField]
    public bool UnlistedNumber = false;

    /// <summary>
    /// Speech is relayed through this entity instead of the telephone
    /// </summary>
    [DataField]
    public EntityUid? Speaker = null;

    /// <summary>
    /// Telephone number for this device
    /// </summary>
    /// <remarks>
    /// For future use - a system for generating and handling telephone numbers has not been implemented yet
    /// </remarks>
    [DataField]
    public int TelephoneNumber = -1;

    /// <summary>
    /// Other telephones that have been linked to this one
    /// </summary>
    [DataField]
    public HashSet<EntityUid> LinkedTelephones = new();

    /// <summary>
    /// Defines the current state the telephone is in
    /// </summary>
    [DataField, AutoNetworkedField]
    public TelephoneState CurrentState = TelephoneState.Idle;

    /// <summary>
    /// Defines the previous state the telephone was in
    /// </summary>
    [DataField, AutoNetworkedField]
    public TelephoneState PreviousState = TelephoneState.Idle;

    /// <summary>
    /// The game tick the current state started
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan StateStartTime;

    /// <summary>
    /// Sets whether the telphone can pick up nearby speech
    /// </summary>
    [DataField]
    public bool Muted = false;

    /// <summary>
    /// The presumed name and/or job of the last person to call this telephone
    /// and the name of the device that they used to do so
    /// </summary>
    [DataField, AutoNetworkedField]
    public TelephoneCallRecord? LastCallerId;
}

/// <summary>
/// A telephone call record.
/// </summary>
/// <param name="CallerId">The name of the person who placed the call.</param>
/// <param name="CallerJob">The job of the person who placed the call.</param>
/// <param name="DeviceId">The name of the device used to make the call.</param>
[Serializable, NetSerializable]
public record struct TelephoneCallRecord(string? CallerId, string? CallerJob, string? DeviceId);

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
    public bool IgnoreRange;    // The source can always reach its target
    public bool ForceConnect;   // The source immediately starts a call with the receiver, potentially interrupting a call that is already in progress
    public bool ForceJoin;      // The source smoothly joins a call in progress, or starts a normal call with the receiver if there is none
    public bool MuteSource;     // Chatter from the source is not transmitted - could be used for eavesdropping when combined with 'ForceJoin'
    public bool MuteReceiver;   // Chatter from the receiver is not transmitted - useful for broadcasting messages to multiple receivers
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
    Grid,       // Can only reach telephones that are on the same grid
    Map,        // Can reach any telephone that is on the same map
    Unlimited,  // Can reach any telephone, across any distance
}
