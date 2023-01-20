using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class DeviceLinkSourceComponent : Component
{
    [DataField("ports", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SourcePortPrototype>))]
    public HashSet<string>? Ports;

    [DataField("registeredSinks")]
    public Dictionary<string, HashSet<EntityUid>> Outputs = new();

    [DataField("linkedPorts")]
    public Dictionary<EntityUid, HashSet<(string source, string sink)>> LinkedPorts = new();

    /// <summary>
    ///     Limits the range devices can be linked across.
    ///     Devices farther than this range can still linked if they are
    ///     on the same apc net.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range = 30f;
}
