using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Adds an entity with an <see cref="AtmosDeviceComponent"/> to a grid's list of atmos devices.
    /// </summary>
    /// <param name="grid">The grid to add the device to.</param>
    /// <param name="device">The device to add.</param>
    /// <returns>True if the device was added, false otherwise.</returns>
    [PublicAPI]
    public bool AddAtmosDevice(Entity<GridAtmosphereComponent?> grid, Entity<AtmosDeviceComponent> device)
    {
        DebugTools.Assert(device.Comp.JoinedGrid == null);
        DebugTools.Assert(Transform(device).GridUid == grid);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.AtmosDevices.Add(device))
            return false;

        device.Comp.JoinedGrid = grid;
        return true;
    }

    /// <summary>
    /// Removes an entity with an <see cref="AtmosDeviceComponent"/> from a grid's list of atmos devices.
    /// </summary>
    /// <param name="grid">The grid to remove the device from.</param>
    /// <param name="device">The device to remove.</param>
    /// <returns>True if the device was removed, false otherwise.</returns>
    [PublicAPI]
    public bool RemoveAtmosDevice(Entity<GridAtmosphereComponent?> grid, Entity<AtmosDeviceComponent> device)
    {
        DebugTools.Assert(device.Comp.JoinedGrid == grid);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.AtmosDevices.Remove(device))
            return false;

        device.Comp.JoinedGrid = null;
        return true;
    }
}
