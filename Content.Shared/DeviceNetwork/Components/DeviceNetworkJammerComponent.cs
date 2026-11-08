using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Allow entities to jam DeviceNetwork packets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[AutoGenerateComponentPause, Access(typeof(DeviceNetworkJammerSystem))]
public sealed partial class DeviceNetworkJammerComponent : Component
{
    /// <summary>
    /// Range where packets will be jammed. This is checked both against the sender and receiver.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 5.0f;

    /// <summary>
    /// Device networks that can be jammed. For a list of default NetworkIds see DeviceNetIdDefaults on Content.Server.
    /// Network ids are not guaranteed to be limited to DeviceNetIdDefaults.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<DeviceNetworkPrototype>> JammableNetworks = [];

    /// <summary>
    /// Device networks frequencies that wont be jammed.
    /// </summary>
    [DataField]
    public HashSet<DeviceFrequency> FrequenciesExcluded = [];

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}
