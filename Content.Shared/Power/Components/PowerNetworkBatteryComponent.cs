using Content.Shared.Collections;
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
public sealed partial class PowerNetworkBatteryComponent : Component, IPowerBattery
{
    [ViewVariables]
    public float LastSupply = 0f;

    [ViewVariables]
    public NodeId Id { get; set; }

    [DataField]
    public bool Enabled { get; set; }

    [DataField]
    public bool Paused { get; set; }

    [DataField]
    public bool CanDischarge { get; set; }

    [DataField]
    public bool CanCharge { get; set; }

    [DataField]
    public float Capacity { get; set; }

    [DataField]
    public float MaxChargeRate { get; set; }

    [DataField]
    public float MaxThroughput { get; set; }

    [DataField]
    [GuidebookData]
    public float MaxSupply { get; set; }

    [DataField]
    public float SupplyRampTolerance { get; set; }

    [DataField]
    public float SupplyRampRate { get; set; }

    [DataField]
    public float Efficiency { get; set; }

    [ViewVariables]
    public float SupplyRampPosition { get; set; }

    [ViewVariables]
    public float CurrentSupply { get; set; }

    [ViewVariables]
    public float CurrentStorage { get; set; }

    [ViewVariables]
    public float CurrentReceiving { get; set; }

    [ViewVariables]
    public float LoadingNetworkDemand { get; set; }

    [ViewVariables]
    public bool SupplyingMarked { get; set; }

    [ViewVariables]
    public bool LoadingMarked { get; set; }

    [ViewVariables]
    public float AvailableSupply { get; set; }

    [ViewVariables]
    public float DesiredPower { get; set; }

    [ViewVariables]
    public float SupplyRampTarget { get; set; }

    [ViewVariables]
    public NodeId LinkedNetworkCharging { get; set; }

    [ViewVariables]
    public NodeId LinkedNetworkDischarging { get; set; }

    [ViewVariables]
    public float MaxEffectiveSupply { get; set; }
}
