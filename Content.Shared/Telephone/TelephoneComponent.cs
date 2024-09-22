using Content.Shared.Holopad;
using Content.Shared.Radio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Threading.Channels;

namespace Content.Shared.Telephone;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class TelephoneComponent : Component
{
    public GameTick StateStartTime;
    public float RingingTimeout = 10;
    public float HangingUpTimeout = 3;

    /// <summary>
    /// The person using the phone
    /// </summary>
    public EntityUid? User;

    /// <summary>
    /// Linked telephone
    /// </summary>
    public EntityUid? LinkedTelephone;

    /// <summary>
    /// Defines the current state the telephone is in
    /// </summary>
    public TelephoneState CurrentState = TelephoneState.Idle;

    /// <summary>
    /// Toggles whether people nearby can participate in the call
    /// </summary>
    public bool IsConferenceCall = false;
}

#region: Telephone events

/// <summary>
/// Raised when one telephone is ringing another
/// </summary>
[ByRefEvent]
public record struct TelephoneOutgoingCallEvent(EntityUid RecipientTelephone);

/// <summary>
/// Raised when one telephone is attempting to call another
/// </summary>
[ByRefEvent]
public record struct TelephoneIncomingCallAttemptEvent(EntityUid CallingTelephone)
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised when one telephone call another
/// </summary>
/// <remarks>
/// Raise a TelephoneCallAttemptEvent on the target telephone before raising this event
/// </remarks>
[ByRefEvent]
public record struct TelephoneIncomingCallEvent(EntityUid CallingTelephone);

/// <summary>
/// Raised when a call commences between two people
/// </summary>
[ByRefEvent]
public record struct TelephoneCallCommencedEvent(EntityUid CallingTelephone, EntityUid RecipientTelephone);

/// <summary>
/// Raised when one telephone hangs up on the other
/// </summary>
[ByRefEvent]
public record struct TelephoneHungUpEvent(EntityUid HangingUpTelephone);

/// <summary>
/// Raised when a telephone becomes idle
/// </summary>
[ByRefEvent]
public record struct TelephoneCallTerminatedEvent();

#endregion

[Serializable, NetSerializable]
public enum TelephoneState : byte
{
    Idle,
    Calling,
    Ringing,
    InCall,
    HangingUp
}
