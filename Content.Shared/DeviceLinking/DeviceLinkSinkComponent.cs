using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

[RegisterComponent]
[NetworkedComponent] // for interactions. Actual state isn't currently synced.
[Access(typeof(SharedDeviceLinkSystem))]
public sealed class DeviceLinkSinkComponent : Component
{
    /// <summary>
    /// The ports this sink has
    /// </summary>
    [DataField("ports", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SinkPortPrototype>))]
    public HashSet<string>? Ports;

    /// <summary>
    /// Used for removing a sink from all linked sources when it gets removed
    /// </summary>
    [DataField("links")]
    public HashSet<EntityUid> LinkedSources = new();
}
