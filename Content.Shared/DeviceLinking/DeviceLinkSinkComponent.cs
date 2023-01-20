using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class DeviceLinkSinkComponent : Component
{
    [DataField("ports", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SinkPortPrototype>))]
    public HashSet<string>? Ports;

    /// <summary>
    /// Used for removing a sink from all linked sources when it gets removed
    /// </summary>
    [DataField("links")]
    public HashSet<EntityUid> LinkedSources = new();
}
