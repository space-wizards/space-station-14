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
    ///     This entity will be grouped with entities that have the same collection name
    /// </summary>
    [DataField("collectionName")]
    public string CollectionName = string.Empty;

    /// <summary>
    ///     Indicates whether the entity is/should be part of a collection
    /// </summary>
    public bool IsCollectionMasterOrChild { get { return CollectionName != string.Empty; } }

    /// <summary>
    ///     Specifies the uid of the master that represents this entity
    /// </summary>
    /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public EntityUid CollectionMaster;

    /// <summary>
    ///     Indicates if this entity represents a group of entities
    /// </summary>
    /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public bool IsCollectionMaster { get { return Owner == CollectionMaster; } }

    /// <summary>
    ///     A list of other entities that are to be represented by this entity
    /// </summary>
    /// /// <remarks>
    ///     Used when grouping multiple entities into a single power monitoring console entry
    /// </remarks>
    [ViewVariables]
    public HashSet<EntityUid> ChildEntities = new();
}
