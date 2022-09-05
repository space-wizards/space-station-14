using System.Linq;
using Content.Shared.DeviceNetwork;
using Robust.Client.Graphics;

namespace Content.Client.NetworkConfigurator;

public sealed class DeviceListSystem : SharedDeviceListSystem
{
    /// <summary>
    /// Toggles the given device lists connection visualisation on and off.
    /// TODO: Implement an overlay that draws a line between the given entity and the entities in the device list
    /// </summary>
    public IEnumerable<EntityUid> GetAllDevices(EntityUid uid, DeviceListComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return new EntityUid[] { };
        }
        return component.Devices;
    }
}
