using Content.Server.NodeContainer;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.Server.Power.Components;

[RegisterComponent, Access(typeof(PowerMonitoringSystem))]
public sealed partial class PowerMonitoringComponent : Component, ISerializationHooks
{
    /// <summary>
    ///     Name of the node that the power monitor will read its sources (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("sourceNode")]
    public string SourceNode = "hv";

    /// <summary>
    ///     Name of the node that the power monitor will read its loads (see <see cref="NodeContainerComponent"/>)
    /// </summary>
    [DataField("loadNode")]
    public string LoadNode = "hv";

    /// <summary>
    ///     The UI key associated with the <see cref="BoundUserInterface"/> that displays the power monitor data
    /// </summary>
    [DataField("key", required: true)]
    private string _keyRaw = default!;

    [ViewVariables]
    public Enum? Key { get; set; }

    void ISerializationHooks.AfterDeserialization()
    {
        var reflectionManager = IoCManager.Resolve<IReflectionManager>();
        if (reflectionManager.TryParseEnumReference(_keyRaw, out var key))
            Key = key;
    }
}
