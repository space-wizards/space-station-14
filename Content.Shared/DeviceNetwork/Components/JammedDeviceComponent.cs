using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Temporary component for <see cref="DeviceNetworkComponent"/> entities that got jammed,
/// meaning they cannot receive or send any packets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(DeviceNetworkSystem))]
public sealed partial class JammedDeviceComponent : Component
{
    /// <summary>
    /// Time until every network type of this device is jammed,
    /// meaning it temporarily cannot send any packets using it.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public Dictionary<ProtoId<DeviceNetworkPrototype>, TimeSpan> JammedTimeouts = new();
}
