using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

[RegisterComponent]
[NetworkedComponent] // for interactions. Actual state isn't currently synced.
[Access(typeof(SharedDeviceLinkSystem))]
public sealed partial class DeviceLinkSinkComponent : Component
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

    /// <summary>
    /// Counts the amount of times a sink has been invoked for severing the link if this counter gets to high
    /// The counter is counted down by one every tick if it's higher than 0
    /// This is for preventing infinite loops
    /// </summary>
    [DataField("invokeCounter")]
    public int InvokeCounter;

    /// <summary>
    /// How high the invoke counter is allowed to get before the links to the sink are removed and the DeviceLinkOverloadedEvent gets raised
    /// If the invoke limit is smaller than 1 the sink can't overload
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("invokeLimit")]
    public int InvokeLimit = 10;
}
