using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Allows to create a device link from some device with a <see cref="DeviceLinkSourceComponent"/> to this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(DeviceLinkSystem))]
public sealed partial class DeviceLinkSinkComponent : Component
{
    /// <summary>
    /// The ports this sink has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<SinkPortPrototype>> Ports = new();

    /// <summary>
    /// Used for removing a sink from all linked sources when this component gets removed.
    /// </summary>
    /// <remarks>
    /// This is not serialized to YAML as it can be inferred from source components.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> LinkedSources = new();

    /// <summary>
    /// The tick <see cref="InvokeCounter"/> was set at. Used to calculate the real value for the current tick.
    /// </summary>
    [Access(typeof(DeviceLinkSystem), Other = AccessPermissions.None)]
    [AutoNetworkedField]
    public GameTick InvokeCounterTick;

    /// <summary>
    /// Counter used to throttle device invocations to avoid infinite loops.
    /// </summary>
    /// <remarks>
    /// This is stored relative to <see cref="InvokeCounterTick"/>. For reading the real value,
    /// <see cref="DeviceLinkSystem.GetEffectiveInvokeCounter"/> should be used.
    /// </remarks>
    [DataField, AutoNetworkedField]
    [Access(typeof(DeviceLinkSystem), Other = AccessPermissions.None)]
    public int InvokeCounter;

    /// <summary>
    /// How high the <see cref="InvokeCounter"/> is allowed to get before the links to the sink
    /// are removed and the <see cref="DeviceLinkOverloadedEvent"/> gets raised.
    /// Values smaller than 1 mean the sink can't overload.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int InvokeLimit = 10;
}
