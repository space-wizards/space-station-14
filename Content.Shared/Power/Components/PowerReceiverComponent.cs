using Content.Shared.Power.Pow3r.Nodes;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Represents a device connected to the APC power network.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerReceiverComponent : Component
{
    [ViewVariables]
    public bool Powered;

    [ViewVariables]
    public EntityUid? Provider;

    /// <summary>
    ///     Amount of charge this needs from an APC per second to function.
    /// </summary>
    [DataField("powerLoad")]
    public float DesiredPower
    {
        get => Load.DesiredPower;
        set => Load.DesiredPower = value;
    }

    /// <summary>
    ///     When false, causes this to appear powered even if not receiving power from an Apc.
    /// </summary>
    [DataField]
    public bool NeedsPower = true;

    /// <summary>
    ///     When false, causes this to never appear powered.
    /// </summary>
    [DataField]
    public bool Enabled
    {
        get => Load.Enabled;
        set => Load.Enabled = value;
    }

    [DataField]
    public bool Paused
    {
        get => Load.Paused;
        set => Load.Paused = value;
    }

    [ViewVariables]
    public float ReceivingPower
    {
        get => Load.ReceivingPower;
        set => Load.ReceivingPower = value;
    }

    [ViewVariables]
    public IPowerLoad Load { get; set; } = new PowerLoad();
}
