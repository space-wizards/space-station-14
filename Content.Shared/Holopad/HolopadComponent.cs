using Content.Shared.Telephone;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holopad;

/// <summary>
/// Holds data pertaining to holopads
/// </summary>
/// <remarks>
/// Holopads also require a <see cref="TelephoneComponent"/> to function
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedHolopadSystem))]
public sealed partial class HolopadComponent : Component
{
    /// <summary>
    /// The entity being projected by the holopad
    /// </summary>
    [DataField]
    public EntityUid? Hologram;

    /// <summary>
    /// The entity using the holopad
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    /// Proto ID for the user's hologram
    /// </summary>
    [DataField]
    public EntProtoId? HologramProtoId;

    /// <summary>
    /// The entity that has locked out the controls of this device
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ControlLockoutOwner = null;

    /// <summary>
    /// The time the control lockout will end
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ControlLockoutEndTime;

    /// <summary>
    /// The time the control lockout cool down will end
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ControlLockoutCoolDownEndTime;

    /// <summary>
    /// The duration that the control lockout will last in seconds
    /// </summary>
    [DataField]
    public TimeSpan ControlLockoutDuration { get; private set; } = TimeSpan.FromSeconds(90);

    /// <summary>
    /// The duration before the controls can be lockout again in seconds
    /// </summary>
    [DataField]
    public TimeSpan ControlLockoutCoolDown { get; private set; } = TimeSpan.FromSeconds(180);
}

#region: Event messages

/// <summary>
///     Data from by the server to the client for the holopad UI
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadBoundInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<NetEntity, string> Holopads;

    public HolopadBoundInterfaceState(Dictionary<NetEntity, string> holopads)
    {
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
public sealed class HolopadEndCallMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadStartBroadcastMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadActivateProjectorMessage : BoundUserInterfaceMessage { }

/// <summary>
///     Triggers the server to send updated power monitoring console data to the client for the single player session
/// </summary>
[Serializable, NetSerializable]
public sealed class HolopadStationAiRequestMessage : BoundUserInterfaceMessage { }

#endregion

/// <summary>
/// Key to the Holopad UI
/// </summary>
[Serializable, NetSerializable]
public enum HolopadUiKey : byte
{
    InteractionWindow,
    InteractionWindowForAi,
    AiActionWindow,
    AiRequestWindow
}
