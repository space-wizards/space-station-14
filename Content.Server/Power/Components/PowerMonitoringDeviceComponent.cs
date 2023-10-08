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
    ///     Indicates whether the location of this entity should be displayed on a power monitoring console
    /// </summary>
    [DataField("locationOnMonitor")]
    public bool LocationOnMonitor = true;
}
