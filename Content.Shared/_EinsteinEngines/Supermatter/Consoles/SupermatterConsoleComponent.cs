using Content.Shared._EinsteinEngines.Supermatter.Consoles;
using Content.Shared._EinsteinEngines.Supermatter.Monitor;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngines.Supermatter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSupermatterConsoleSystem))]
public sealed partial class SupermatterConsoleComponent : Component
{
    /// <summary>
    /// The current entity of interest (selected via the console UI)
    /// </summary>
    [ViewVariables]
    public NetEntity? FocusSupermatter;

    /// <summary>
    /// A list of all the supermatters that will be used to populate the nav map
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<SupermatterNavMapData> Supermatters = new();
}

[Serializable, NetSerializable]
public struct SupermatterNavMapData
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
    /// Populate the supermatter console nav map with a single entity
    /// </summary>
    public SupermatterNavMapData(NetEntity netEntity, NetCoordinates netCoordinates)
    {
        NetEntity = netEntity;
        NetCoordinates = netCoordinates;
    }
}

[Serializable, NetSerializable]
public struct SupermatterFocusData
{
    /// <summary>
    /// Focus entity
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Populates the supermatter console focus entry with supermatter data
    /// </summary>
    public SupermatterFocusData
        (NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class SupermatterConsoleBoundInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// A list of all supermatters
    /// </summary>
    public SupermatterConsoleEntry[] Supermatters;

    /// <summary>
    /// Data for the UI focus (if applicable)
    /// </summary>
    public SupermatterFocusData? FocusData;

    /// <summary>
    /// Sends data from the server to the client to populate the atmos monitoring console UI
    /// </summary>
    public SupermatterConsoleBoundInterfaceState(SupermatterConsoleEntry[] supermatters, SupermatterFocusData? focusData)
    {
        Supermatters = supermatters;
        FocusData = focusData;
    }
}

[Serializable, NetSerializable]
public struct SupermatterConsoleEntry
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
    /// Name of the entity
    /// </summary>
    public string EntityName;

    /// <summary>
    /// Current alert level
    /// </summary>
    public SupermatterStatusType EntityStatus;

    /// <summary>
    /// Used to populate the supermatter console UI with data from a single supermatter
    /// </summary>
    public SupermatterConsoleEntry
        (NetEntity entity,
        NetCoordinates coordinates,
        string entityName,
        SupermatterStatusType status)
    {
        NetEntity = entity;
        NetCoordinates = coordinates;
        EntityName = entityName;
        EntityStatus = status;
    }
}

[Serializable, NetSerializable]
public sealed class SupermatterConsoleFocusChangeMessage : BoundUserInterfaceMessage
{
    public NetEntity? FocusSupermatter;

    /// <summary>
    /// Used to inform the server that the specified focus for the atmos monitoring console has been changed by the client
    /// </summary>
    public SupermatterConsoleFocusChangeMessage(NetEntity? focusSupermatter)
    {
        FocusSupermatter = focusSupermatter;
    }
}

[NetSerializable, Serializable]
public enum SupermatterConsoleVisuals
{
    ComputerLayerScreen,
}

/// <summary>
/// UI key associated with the supermatter monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum SupermatterConsoleUiKey
{
    Key
}
