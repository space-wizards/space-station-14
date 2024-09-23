using Content.Shared.Telephone;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedHolopadSystem))]
public sealed partial class HolopadComponent : Component
{
    /// <summary>
    /// The entity being projected by the holopad
    /// </summary>
    [ViewVariables]
    public EntityUid? Hologram;

    /// <summary>
    /// Proto ID for the user's hologram
    /// </summary>
    [DataField]
    public EntProtoId? HologramProtoId;
}

/// <summary>
///     Data from by the server to the client for the holopad UI
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadBoundInterfaceState : BoundUserInterfaceState
{
    public readonly TelephoneState State;
    public readonly Dictionary<NetEntity, string> Holopads;

    public HolopadBoundInterfaceState(TelephoneState state, Dictionary<NetEntity, string> holopads)
    {
        State = state;
        Holopads = holopads;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadStartNewCallMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Receiver;

    public HolopadStartNewCallMessage(NetEntity receiver)
    {
        Receiver = receiver;
    }
}

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadAnswerCallMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadHangUpOnCallMessage : BoundUserInterfaceMessage { }


/// <summary>
/// A networked event raised when the visual state of a hologram is being updated
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadHologramVisualsUpdateEvent : EntityEventArgs
{
    /// <summary>
    /// The hologram being updated
    /// </summary>
    public readonly NetEntity Hologram;

    /// <summary>
    /// The target the hologram is copying
    /// </summary>
    public readonly NetEntity Target;

    public HolopadHologramVisualsUpdateEvent(NetEntity hologram, NetEntity target)
    {
        Hologram = hologram;
        Target = target;
    }
}

/// <summary>
/// Key to the Holopad UI
/// </summary>
[Serializable, NetSerializable]
public enum HolopadUiKey : byte
{
    Key
}
