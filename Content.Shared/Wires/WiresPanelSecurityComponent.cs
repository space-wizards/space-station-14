using Robust.Shared.GameStates;

namespace Content.Shared.Wires;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedWiresSystem))]
[AutoGenerateComponentState]
public sealed partial class WiresPanelSecurityComponent : Component
{
    /// <summary>
    ///     A verbal description of the wire panel's current security level
    /// </summary>
    [DataField("examine")]
    [AutoNetworkedField]
    public string? Examine = default!;

    /// <summary>
    ///     Determines whether the wiring is accessible to hackers or not
    /// </summary>
    [DataField("wiresAccessible")]
    [AutoNetworkedField]
    public bool WiresAccessible = true;

    /// <summary>
    ///     Determines whether the device can be welded shut or not
    /// </summary>
    /// <remarks>
    ///     Should be set false when you need to weld/unweld something to/from the wire panel
    /// </remarks>
    [DataField("weldingAllowed")]
    [AutoNetworkedField]
    public bool WeldingAllowed = true;

    /// <summary>
    ///     Name of the construction graph to which specifies all the security upgrades for the wires panel
    /// </summary>
    [DataField("startGraph", required: true)]
    [AutoNetworkedField]
    public string StartGraph = string.Empty;

    /// <summary>
    ///     Name of the node to use on the starting construction graph
    /// </summary>
    [DataField("startNode", required: true)]
    [AutoNetworkedField]
    public string StartNode = string.Empty;

    /// <summary>
    ///     Name of the construction graph to use to when all security features are removed from the wires panel
    /// </summary>
    [DataField("baseGraph", required: true)]
    [AutoNetworkedField]
    public string BaseGraph = string.Empty;

    /// <summary>
    ///     Name of the node to use on the base construction graph
    /// </summary>
    [DataField("baseNode", required: true)]
    [AutoNetworkedField]
    public string BaseNode = string.Empty;
}
