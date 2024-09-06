using Content.Shared.Atmos.Consoles;
using Content.Shared.Atmos.Monitor;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAtmosAlertsComputerSystem))]
public sealed partial class AtmosAlertsComputerComponent : Component
{
    /// <summary>
    /// The current entity of interest (selected via the console UI)
    /// </summary>
    [ViewVariables]
    public NetEntity? FocusDevice;

    /// <summary>
    /// A list of all the atmos devices that will be used to populate the nav map
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<AtmosAlertsDeviceNavMapData> AtmosDevices = new();

    /// <summary>
    /// A list of all the air alarms that have had their alerts silenced on this particular console
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> SilencedDevices = new();
}

[Serializable, NetSerializable]
public struct AtmosAlertsDeviceNavMapData
{
    /// <summary>
    /// The entity in question
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Location of the entity
    /// </summary>
    public NetCoordinates NetCoordinates;

    /// <summary>
    /// Used to determine what map icons to use
    /// </summary>
    public AtmosAlertsComputerGroup Group;

    /// <summary>
    /// Populate the atmos monitoring console nav map with a single entity
    /// </summary>
    public AtmosAlertsDeviceNavMapData(NetEntity netEntity, NetCoordinates netCoordinates, AtmosAlertsComputerGroup group)
    {
        NetEntity = netEntity;
        NetCoordinates = netCoordinates;
        Group = group;
    }
}

[Serializable, NetSerializable]
public struct AtmosAlertsFocusDeviceData
{
    /// <summary>
    /// Focus entity
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Temperature (K) and related alert state
    /// </summary>
    public (float, AtmosAlarmType) TemperatureData;

    /// <summary>
    /// Pressure (kPA) and related alert state
    /// </summary>
    public (float, AtmosAlarmType) PressureData;

    /// <summary>
    /// Moles, percentage, and related alert state, for all detected gases 
    /// </summary>
    public Dictionary<Gas, (float, float, AtmosAlarmType)> GasData;

    /// <summary>
    /// Populates the atmos monitoring console focus entry with atmospheric data
    /// </summary>
    public AtmosAlertsFocusDeviceData
        (NetEntity netEntity,
        (float, AtmosAlarmType) temperatureData,
        (float, AtmosAlarmType) pressureData,
        Dictionary<Gas, (float, float, AtmosAlarmType)> gasData)
    {
        NetEntity = netEntity;
        TemperatureData = temperatureData;
        PressureData = pressureData;
        GasData = gasData;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosAlertsComputerBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// A list of all air alarms
    /// </summary>
    public AtmosAlertsComputerEntry[] AirAlarms;

    /// <summary>
    /// A list of all fire alarms
    /// </summary>
    public AtmosAlertsComputerEntry[] FireAlarms;

    /// <summary>
    /// Data for the UI focus (if applicable)
    /// </summary>
    public AtmosAlertsFocusDeviceData? FocusData;

    /// <summary>
    /// Sends data from the server to the client to populate the atmos monitoring console UI
    /// </summary>
    public AtmosAlertsComputerBoundInterfaceState(AtmosAlertsComputerEntry[] airAlarms, AtmosAlertsComputerEntry[] fireAlarms, AtmosAlertsFocusDeviceData? focusData)
    {
        AirAlarms = airAlarms;
        FireAlarms = fireAlarms;
        FocusData = focusData;
    }
}

[Serializable, NetSerializable]
public struct AtmosAlertsComputerEntry
{
    /// <summary>
    /// The entity in question
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Location of the entity
    /// </summary>
    public NetCoordinates Coordinates;

    /// <summary>
    /// The type of entity
    /// </summary>
    public AtmosAlertsComputerGroup Group;

    /// <summary>
    /// Current alarm state
    /// </summary>
    public AtmosAlarmType AlarmState;

    /// <summary>
    /// Localised device name
    /// </summary>
    public string EntityName;

    /// <summary>
    /// Device network address
    /// </summary>
    public string Address;

    /// <summary>
    /// Used to populate the atmos monitoring console UI with data from a single air alarm
    /// </summary>
    public AtmosAlertsComputerEntry
        (NetEntity entity,
        NetCoordinates coordinates,
        AtmosAlertsComputerGroup group,
        AtmosAlarmType alarmState,
        string entityName,
        string address)
    {
        NetEntity = entity;
        Coordinates = coordinates;
        Group = group;
        AlarmState = alarmState;
        EntityName = entityName;
        Address = address;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosAlertsComputerFocusChangeMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusDevice;

    /// <summary>
    /// Used to inform the server that the specified focus for the atmos monitoring console has been changed by the client
    /// </summary>
    public AtmosAlertsComputerFocusChangeMessage(NetEntity? focusDevice)
    {
        FocusDevice = focusDevice;
    }
}

[Serializable, NetSerializable]
public sealed class AtmosAlertsComputerDeviceSilencedMessage : BoundUserInterfaceMessage
{
    public NetEntity AtmosDevice;
    public bool SilenceDevice = true;

    /// <summary>
    /// Used to inform the server that the client has silenced alerts from the specified device to this atmos monitoring console 
    /// </summary>
    public AtmosAlertsComputerDeviceSilencedMessage(NetEntity atmosDevice, bool silenceDevice = true)
    {
        AtmosDevice = atmosDevice;
        SilenceDevice = silenceDevice;
    }
}

/// <summary>
/// List of all the different atmos device groups
/// </summary>
public enum AtmosAlertsComputerGroup
{
    Invalid,
    AirAlarm,
    FireAlarm,
}

[NetSerializable, Serializable]
public enum AtmosAlertsComputerVisuals
{
    ComputerLayerScreen,
}

/// <summary>
/// UI key associated with the atmos monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum AtmosAlertsComputerUiKey
{
    Key
}
