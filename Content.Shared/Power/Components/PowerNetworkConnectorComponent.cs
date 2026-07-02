using Content.Shared.Power.NodeGroups;

namespace Content.Shared.Power.Components;

[RegisterComponent]
public sealed partial class PowerNetworkConnectorComponent : Component
{
    /// <summary>
    /// Voltage of this power network connector,
    /// determines the type of powernet it will try to connect to.
    /// </summary>
    [DataField]
    public Voltage? Voltage;

    /// <summary>
    /// Current PowerNet this connector is a part of.
    /// </summary>
    [ViewVariables]
    public PowerNet? Net;

    /// <summary>
    /// The name of the node this power network connector will try to connect to.
    /// Set to null in order to connect to everything.
    /// </summary>
    [DataField("node")]
    public string? NodeId;

    /// <summary>
    /// A dictionary to add multiple nodes with specific voltages to a single entity.
    /// Useful for things like SMES, APC, Substations, and other power net devices with multiple cable nodes.
    /// </summary>
    [DataField]
    public Dictionary<string, Voltage>? Voltages;

    /// <summary>
    /// A set of nets this device is a part of.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, PowerNet?>? Nets;
}
