using Content.Shared.Guidebook;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerSupplierComponent : Component
{
    [DataField]
    public Voltage Voltage;

    [DataField("supplyRate")]
    [GuidebookData]
    public float MaxSupply
    {
        get => Supply.MaxSupply;
        set => Supply.MaxSupply =  value;
    }

    [DataField]
    public bool Enabled
    {
        get => Supply.Enabled;
        set => Supply.Enabled =  value;
    }

    [DataField]
    public bool Paused
    {
        get => Supply.Paused;
        set => Supply.Paused =  value;
    }

    [DataField]
    public float SupplyRampRate
    {
        get => Supply.SupplyRampRate;
        set => Supply.SupplyRampRate =  value;
    }

    [DataField]
    public float SupplyRampTolerance
    {
        get => Supply.SupplyRampTolerance;
        set => Supply.SupplyRampTolerance =  value;
    }

    [ViewVariables]
    public float CurrentSupply => Supply.CurrentSupply;

    [ViewVariables]
    public float SupplyRampTarget => Supply.SupplyRampTarget;

    [ViewVariables]
    public float SupplyRampPosition => Supply.SupplyRampPosition;

    [ViewVariables]
    public float AvailableSupply => Supply.AvailableSupply;

    [ViewVariables]
    public IPowerSupply Supply = default!;
}
