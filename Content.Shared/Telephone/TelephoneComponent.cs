using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

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
    public TimeSpan NextToneTime;

    /// <summary>
    /// Toggles whether people nearby can participate in the call
    /// </summary>
    [DataField]
    public bool IsConferenceCall = false;

    /// <summary>
    /// The person using the phone
    /// </summary>
    [ViewVariables]
    public EntityUid? User;

    /// <summary>
    /// Linked telephone
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedTelephone;

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
}

#region: Telephone events

/// <summary>
/// Raised when one telephone is attempting to call another
/// </summary>
[ByRefEvent]
public record struct TelephoneCallAttemptEvent(EntityUid Source, EntityUid Receiver, EntityUid? User)
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised when one telephone is calling another
/// </summary>
[ByRefEvent]
public record struct TelephoneCallEvent(EntityUid Source, EntityUid Receiver, EntityUid? User);

/// <summary>
/// Raised when a call commences between two telephones
/// </summary>
[ByRefEvent]
public record struct TelephoneCallCommencedEvent(EntityUid Source, EntityUid Receiver);

/// <summary>
/// Raised when a telephone hangs up
/// </summary>
[ByRefEvent]
public record struct TelephoneHungUpEvent(EntityUid Source);

/// <summary>
/// Raised when a telephone becomes idle
/// </summary>
[ByRefEvent]
public record struct TelephoneCallTerminatedEvent();


[ByRefEvent]
public readonly record struct TelephoneMessageReceivedEvent(string Message, EntityUid MessageSource, EntityUid TelephoneSource, MsgChatMessage ChatMsg);


#endregion

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
    HangingUp
}
