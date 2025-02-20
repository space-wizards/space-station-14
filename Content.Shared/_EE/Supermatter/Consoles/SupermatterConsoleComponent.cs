using Content.Shared._EE.Supermatter.Consoles;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._EE.Supermatter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSupermatterConsoleSystem))]
public sealed partial class SupermatterConsoleComponent : Component
{
    /// <summary>
    /// The current entity of interest (selected via the console UI)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public NetEntity? FocusSupermatter;
}

[Serializable, NetSerializable]
public struct SupermatterFocusData(
    NetEntity netEntity,
    GasMixture gasStorage,
    float integrity,
    float heatHealing,
    float power,
    float powerLoss,
    float radiation,
    float temperatureLimit,
    float heatModifier,
    float gasHeatModifier,
    float absorptionRatio)
{
    /// <summary>
    /// Focus entity
    /// </summary>
    public NetEntity NetEntity = netEntity;

    public GasMixture GasStorage = gasStorage;

    public float Integrity = integrity;

    public float HeatHealing = heatHealing;

    public float Power = power;

    public float PowerLoss = powerLoss;

    public float Radiation = radiation;

    public float TemperatureLimit = temperatureLimit;

    public float HeatModifier = heatModifier;

    public float GasHeatModifier = gasHeatModifier;

    public float AbsorptionRatio = absorptionRatio;
}

[Serializable, NetSerializable]
public sealed class SupermatterConsoleBoundInterfaceState(SupermatterConsoleEntry[] supermatters, SupermatterFocusData? focusData) : BoundUserInterfaceState
{
    /// <summary>
    /// A list of all supermatters
    /// </summary>
    public SupermatterConsoleEntry[] Supermatters = supermatters;

    /// <summary>
    /// Data for the UI focus (if applicable)
    /// </summary>
    public SupermatterFocusData? FocusData = focusData;
}

[Serializable, NetSerializable]
public struct SupermatterConsoleEntry(NetEntity entity, string entityName, SupermatterStatusType status)
{
    /// <summary>
    /// The entity in question
    /// </summary>
    public NetEntity NetEntity = entity;

    /// <summary>
    /// Name of the entity
    /// </summary>
    public string EntityName = entityName;

    /// <summary>
    /// Current warning level
    /// </summary>
    public SupermatterStatusType EntityStatus = status;
}

[Serializable, NetSerializable]
public sealed class SupermatterConsoleFocusChangeMessage(NetEntity? focusSupermatter) : BoundUserInterfaceMessage
{
    public NetEntity? FocusSupermatter = focusSupermatter;
}

[NetSerializable, Serializable]
public enum SupermatterConsoleVisuals
{
    ComputerLayerScreen
}

/// <summary>
/// UI key associated with the supermatter monitoring console
/// </summary>
[Serializable, NetSerializable]
public enum SupermatterConsoleUiKey
{
    Key
}
