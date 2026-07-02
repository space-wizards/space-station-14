using Content.Shared.Collections;
using Content.Shared.Guidebook;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerSupplierComponent : Component, IPowerSupply
{
    [DataField("supplyRate")]
    [GuidebookData]
    public float MaxSupply { get; set; }

    [DataField]
    public Voltage Voltage;

    [ViewVariables]
    public NodeId Id { get; set; }

    [DataField]
    public bool Enabled { get; set; }

    [DataField]
    public bool Paused { get; set; }

    [DataField]
    public float SupplyRampRate { get; set; }

    [DataField]
    public float SupplyRampTolerance { get; set; }

    [ViewVariables]
    public float CurrentSupply { get; set; }

    [ViewVariables]
    public float SupplyRampTarget { get; set; }

    [ViewVariables]
    public float SupplyRampPosition { get; set; }

    [ViewVariables]
    public NodeId LinkedNetwork { get; set; }

    [ViewVariables]
    public float AvailableSupply { get; set; }
}
