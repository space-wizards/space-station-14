using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Power;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// System that disconnects and reconnects devices depending on their power state.
/// </summary>
public sealed partial class DeviceNetworkRequiresPowerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<DeviceNetworkRequiresPowerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            _deviceNetwork.ConnectDevice(ent.Owner);
        else
            _deviceNetwork.DisconnectDevice(ent.Owner);
    }
}
