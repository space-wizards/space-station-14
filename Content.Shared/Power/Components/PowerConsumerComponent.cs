using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
///     Draws power directly from an MV or HV wire it is on top of.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerConsumerComponent : Component
{
    [DataField]
    public string NodeId = "input";

    /// <summary>
    ///     How much power this needs to be fully powered.
    /// </summary>
    [DataField("drawRate")]
    public float DesiredPower
    {
        get => Load.DesiredPower;
        set => Load.DesiredPower = value;
    }

    [DataField]
    public bool ShowInMonitor = true;

    /// <summary>
    ///     How much power this is currently receiving from <see cref="PowerSupplierComponent"/>s.
    /// </summary>
    [ViewVariables]
    public float ReceivingPower => Load.ReceivingPower;

    [ViewVariables]
    public float LastReceived = float.NaN;

    [DataField]
    public bool Enabled
    {
        get => Load.Paused;
        set => Load.Paused = value;
    }

    [DataField]
    public bool Paused
    {
        get => Load.Paused;
        set => Load.Paused = value;
    }

    [ViewVariables]
    public IPowerLoad Load = new PowerLoad();
}
