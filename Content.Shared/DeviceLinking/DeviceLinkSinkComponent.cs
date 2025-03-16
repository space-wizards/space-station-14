using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    [DataField]
    public HashSet<ProtoId<SinkPortPrototype>> Ports = new();

    /// <summary>
    /// Used for removing a sink from all linked sources when this component gets removed.
    /// This is not serialized to yaml as it can be inferred from source components.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> LinkedSources = new();

    /// <summary>
    /// Counts the amount of times a sink has been invoked for severing the link if this counter gets to high
    /// The counter is counted down by one every tick if it's higher than 0
    /// This is for preventing infinite loops
    /// </summary>
    [DataField]
    public int InvokeCounter;

    /// <summary>
    /// How high the invoke counter is allowed to get before the links to the sink are removed and the DeviceLinkOverloadedEvent gets raised
    /// If the invoke limit is smaller than 1 the sink can't overload
    /// </summary>
    [DataField]
    public int InvokeLimit = 10;
}
