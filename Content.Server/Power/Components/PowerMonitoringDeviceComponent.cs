using Content.Server.NodeContainer;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <summary>
///     Used to flag any entities that should appear on a power monitoring console
/// </summary>
[RegisterComponent, Access(typeof(PowerMonitoringConsoleSystem))]
public sealed partial class PowerMonitoringDeviceComponent : SharedPowerMonitoringDeviceComponent
{
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
    ///     Indicates whether the entity should be grouped with alike entities that are connected
    /// </summary>
    [DataField("joinAlikeEntities")]
    public bool JoinAlikeEntities = false;

    /// <summary>
    ///     Specifies the uid of the exemplar that represents this entity
    /// </summary>
    /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public EntityUid ExemplarUid;

    /// <summary>
    ///     Indicates if this entity is an exemplar that represents a group of entities
    /// </summary>
    /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public bool IsExemplar { get { return Owner == ExemplarUid; } }

    /// <summary>
    ///     A list of other entities that are to be represented by this entity
    /// </summary>
    /// /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public HashSet<EntityUid> ChildEntities = new();
}
