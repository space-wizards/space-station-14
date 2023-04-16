using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

[RegisterComponent]
[NetworkedComponent] // for interactions. Actual state isn't currently synced.
[Access(typeof(SharedDeviceLinkSystem))]
public sealed class DeviceLinkSourceComponent : Component
{
    /// <summary>
    /// The ports the device link source sends signals from
    /// </summary>
    [DataField("ports", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SourcePortPrototype>))]
    public HashSet<string>? Ports;

    /// <summary>
    /// A list of sink uids that got linked for each port
    /// </summary>
    [DataField("registeredSinks")]
    public Dictionary<string, HashSet<EntityUid>> Outputs = new();

    /// <summary>
    /// The list of source to sink ports for each linked sink entity for easier managing of links
    /// </summary>
    [DataField("linkedPorts")]
    public Dictionary<EntityUid, HashSet<(string source, string sink)>> LinkedPorts = new();

    /// <summary>
    ///     Limits the range devices can be linked across.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 30f;
}
