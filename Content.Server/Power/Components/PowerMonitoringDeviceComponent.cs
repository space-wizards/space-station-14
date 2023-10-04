using Content.Server.NodeContainer;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.Power.Components;

[RegisterComponent, Access(typeof(PowerMonitoringConsoleSystem))]
public sealed partial class PowerMonitoringDeviceComponent : Component
{
    /// <summary>
    ///     Name of the node that the power monitor will read its sources (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("sourceNode")]
    public string SourceNode = string.Empty;

    /// <summary>
    ///     Name of the node that the power monitor will read its loads (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("loadNode")]
    public string LoadNode = string.Empty;
}
