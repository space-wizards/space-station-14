using Content.Shared.Guidebook;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Shared.Power.Components;

/// <summary>
///     Glue component that manages the pow3r network node for batteries that are connected to the power network.
/// </summary>
/// <remarks>
///     This needs components like <see cref="BatteryChargerComponent"/> to work correctly,
///     and battery storage should be handed off to components like <see cref="BatteryComponent"/>.
/// </remarks>
[RegisterComponent]
public sealed partial class PowerNetworkBatteryComponent : Component
{
    [ViewVariables]
    public float LastSupply = 0f;

    [DataField]
    public bool Enabled
    {
        get => Battery.Enabled;
        set => Battery.Enabled = value;
    }

    [DataField]
    public bool Paused
    {
        get => Battery.Paused;
        set => Battery.Paused = value;
    }

    [DataField]
    public bool CanCharge
    {
        get => Battery.CanCharge;
        set => Battery.CanCharge = value;
    }

    [DataField]
    public bool CanDischarge
    {
        get => Battery.CanDischarge;
        set => Battery.CanDischarge = value;
    }

    [DataField]
    public float Capacity
    {
        get => Battery.Capacity;
        set => Battery.Capacity = value;
    }

    [DataField]
    public float MaxChargeRate
    {
        get => Battery.MaxChargeRate;
        set => Battery.MaxChargeRate = value;
    }

    [DataField]
    public float MaxThroughput
    {
        get => Battery.MaxThroughput;
        set => Battery.MaxThroughput = value;
    }

    [DataField]
    [GuidebookData]
    public float MaxSupply
    {
        get => Battery.MaxSupply;
        set => Battery.MaxSupply = value;
    }

    [DataField]
    public float SupplyRampTolerance
    {
        get => Battery.SupplyRampTolerance;
        set => Battery.SupplyRampTolerance = value;
    }

    [DataField]
    public float SupplyRampRate
    {
        get => Battery.SupplyRampRate;
        set => Battery.SupplyRampRate = value;
    }

    [DataField]
    public float Efficiency
    {
        get => Battery.Efficiency;
        set => Battery.Efficiency = value;
    }

    [ViewVariables]
    public float SupplyRampPosition
    {
        get => Battery.SupplyRampPosition;
        set => Battery.SupplyRampPosition = value;
    }

    [ViewVariables]
    public float CurrentSupply
    {
        get => Battery.CurrentSupply;
        set => Battery.CurrentSupply = value;
    }

    [ViewVariables]
    public float CurrentStorage
    {
        get => Battery.CurrentStorage;
        set => Battery.CurrentStorage = value;
    }

    [ViewVariables]
    public float CurrentReceiving
    {
        get => Battery.CurrentReceiving;
        set => Battery.CurrentReceiving = value;
    }

    [ViewVariables]
    public float LoadingNetworkDemand
    {
        get => Battery.LoadingNetworkDemand;
        set => Battery.LoadingNetworkDemand = value;
    }

    [ViewVariables]
    public bool SupplyingMarked
    {
        get => Battery.SupplyingMarked;
        set => Battery.SupplyingMarked = value;
    }

    [ViewVariables]
    public bool LoadingMarked
    {
        get => Battery.LoadingMarked;
        set => Battery.LoadingMarked = value;
    }

    [ViewVariables]
    public float AvailableSupply
    {
        get => Battery.AvailableSupply;
        set => Battery.AvailableSupply = value;
    }

    [ViewVariables]
    public float DesiredPower
    {
        get => Battery.DesiredPower;
        set => Battery.DesiredPower = value;
    }

    [ViewVariables]
    public float SupplyRampTarget
    {
        get => Battery.SupplyRampTarget;
        set => Battery.SupplyRampTarget = value;
    }

    [ViewVariables]
    public float MaxEffectiveSupply
    {
        get => Battery.MaxEffectiveSupply;
        set => Battery.MaxEffectiveSupply = value;
    }

    [ViewVariables]
    public IPowerBattery Battery = default!;
}
