using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedHolopadSystem))]
public sealed partial class HolopadComponent : Component
{
    public float CurrentCallDuration = 0;
    public float CallingTimeout = 0;
    public EntityUid? CurrentUser;
    public float InteractionDistance = 1.5f;
    public EntityUid? LinkedHolopad;
    public EntityUid? HoloCallRecipient;
    public EntProtoId? HoloCloneProtoId;
    public EntityUid? Hologram;
    public HolopadState CurrentState = HolopadState.Inactive;
}

/// <summary>
///     Data from by the server to the client for the holopad UI
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadBoundInterfaceState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, string> Holopads;

    public HolopadBoundInterfaceState(Dictionary<NetEntity, string> holopads)
    {
        Holopads = holopads;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadMessage : BoundUserInterfaceMessage
{
    public NetEntity Caller;
    public NetEntity Recipient;

    public HolopadMessage(NetEntity caller, NetEntity recipient)
    {
        Caller = caller;
        Recipient = recipient;
    }
}

[ByRefEvent]
public readonly record struct HoloCallInitiationEvent();

[ByRefEvent]
public readonly record struct HoloCallTerminationEvent();

[Serializable, NetSerializable]
public enum HolopadState : byte
{
    Inactive,
    Calling,
    Ringing,
    Active,
    HangingUp
}

[Serializable, NetSerializable]
public enum HolopadVisualState : byte
{
    State
}

[Serializable, NetSerializable]
public enum HolopadUiKey : byte
{
    Key
}
