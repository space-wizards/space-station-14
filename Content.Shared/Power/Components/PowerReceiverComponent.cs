using Content.Shared.Collections;
using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PowerReceiverComponent : Component, IPowerLoad
{
    [ViewVariables]
    public bool Powered;

    [ViewVariables]
    public EntityUid? Provider;

    /// <summary>
    ///     Amount of charge this needs from an APC per second to function.
    /// </summary>
    [DataField("powerLoad")]
    public float DesiredPower { get; set; }

    /// <summary>
    ///     When false, causes this to appear powered even if not receiving power from an Apc.
    /// </summary>
    [DataField]
    public bool NeedsPower;

    /// <summary>
    ///     When false, causes this to never appear powered.
    /// </summary>
    [DataField]
    public bool Enabled { get; set; } = true;

    [ViewVariables]
    public NodeId Id { get; set; }

    [DataField]
    public bool Paused { get; set; }

    [ViewVariables]
    public float ReceivingPower { get; set; }

    [ViewVariables]
    public NodeId LinkedNetwork { get; set; }
}
