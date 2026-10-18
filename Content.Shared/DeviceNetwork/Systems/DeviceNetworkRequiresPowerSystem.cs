using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkRequiresPowerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceSystem = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<DeviceNetworkRequiresPowerComponent> ent, ref PowerChangedEvent args)
    {
        _deviceSystem.SetEnabled(ent.Owner, args.Powered);
    }
}
