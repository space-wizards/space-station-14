using System.Linq;
using Content.Shared.DeviceNetwork;
using Robust.Client.Graphics;

namespace Content.Client.NetworkConfigurator;

public sealed class DeviceListSystem : SharedDeviceListSystem
{

    public IEnumerable<EntityUid> GetAllDevices(EntityUid uid, DeviceListComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return new EntityUid[] { };
        }
        return component.Devices;
    }
}
