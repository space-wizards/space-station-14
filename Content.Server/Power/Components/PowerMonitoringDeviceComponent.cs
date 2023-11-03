using Content.Server.NodeContainer;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <summary>
///     Used to flag any entities that should appear on a power monitoring console
/// </summary>
[RegisterComponent, Access(typeof(PowerMonitoringConsoleSystem))]
public sealed partial class PowerMonitoringDeviceComponent : Component
{
    /// <summary>
    ///    Determines what power monitoring group this entity should belong to 
    /// </summary>
    [DataField("group", required: true)]
    public PowerMonitoringConsoleGroup Group;

    /// <summary>
    ///     Name of the node that this device draws its power from (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("sourceNode")]
    public string SourceNode = string.Empty;

    /// <summary>
    ///     Name of the node that this device distributes power to (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("loadNode")]
    public string LoadNode = string.Empty;

    /// <summary>
    ///     Names of the nodes that this device can potentially distributes power to (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("loadNodes")]
    public List<string>? LoadNodes;

    /// <summary>
    ///     Indicates whether the location of this entity should be displayed on a power monitoring console
    /// </summary>
    [DataField("locationOnMonitor")]
    public bool LocationOnMonitor = true;

    /// <summary>
    ///     Indicates whether the entity should be grouped with alike entities that are connected
    /// </summary>
    [DataField("groupWithAlikeEnitites")]
    public bool GroupWithAlikeEnitites = false;

    /// <summary>
    ///     The group ID to which the entity belongs
    /// </summary>
    /// <remarks>
    ///     Used to group multiple entities into a single power monitoring console entry
    ///     Only used if 'GroupWithAlikeEnitites' is true
    /// </remarks>
    public EntityUid GroupId;
}
