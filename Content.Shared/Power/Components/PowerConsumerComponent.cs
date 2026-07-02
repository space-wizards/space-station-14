using Content.Shared.Collections;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
///     Draws power directly from an MV or HV wire it is on top of.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerConsumerComponent : Component, IPowerLoad
{
    /// <summary>
    ///     How much power this needs to be fully powered.
    /// </summary>
    [DataField("drawRate")]
    public float DesiredPower { get; set; }

    [DataField]
    public bool ShowInMonitor { get; set; } = true;

    /// <summary>
    ///     How much power this is currently receiving from <see cref="PowerSupplierComponent"/>s.
    /// </summary>
    [ViewVariables]
    public float ReceivingPower { get; set; }

    [ViewVariables]
    public float LastReceived = float.NaN;

    [ViewVariables]
    public NodeId Id { get; set; }

    [DataField]
    public bool Enabled { get; set; }

    [DataField]
    public bool Paused { get; set; }

    [ViewVariables]
    public NodeId LinkedNetwork { get; set; }
}
