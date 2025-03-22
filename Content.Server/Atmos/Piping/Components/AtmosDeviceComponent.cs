using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;

namespace Content.Server.Atmos.Piping.Components;

/// <summary>
///     Component for atmos devices which are updated in line with atmos, as part of a <see cref="GridAtmosphereComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class AtmosDeviceComponent : Component
{
    /// <summary>
    ///     If true, this device must be anchored before it will receive any AtmosDeviceUpdateEvents.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool RequireAnchored = true;

    /// <summary>
    ///     If true, update even when there is no grid atmosphere. Normally, atmos devices only
    ///     update when inside a grid atmosphere, because they work with gases in the environment
    ///     and won't do anything useful if there is no environment. This is useful for devices
    ///     like gas canisters whose contents can still react if the canister itself is not inside
    ///     a grid atmosphere.
    /// </summary>
    [DataField]
    public bool JoinSystem = false;

    /// <summary>
    ///     If non-null, the grid that this device is part of.
    /// </summary>
    [ViewVariables]
    public EntityUid? JoinedGrid = null;

    /// <summary>
    ///     Indicates that a device is not on a grid atmosphere but still being updated.
    /// </summary>
    [ViewVariables]
    public bool JoinedSystem = false;

    [ViewVariables]
    public TimeSpan LastProcess = TimeSpan.Zero;
}

/// <summary>
/// Raised directed on an atmos device as part of the atmos update loop when the device should do processing.
/// Use this for atmos devices instead of <see cref="EntitySystem.Update"/>.
/// </summary>
[ByRefEvent]
public readonly struct AtmosDeviceUpdateEvent(float dt, Entity<GridAtmosphereComponent, GasTileOverlayComponent>? grid, Entity<MapAtmosphereComponent?>? map)
{
    /// <summary>
    /// Time elapsed since last update, in seconds. Multiply values used in the update handler
    /// by this number to make them tickrate-invariant. Use this number instead of AtmosphereSystem.AtmosTime.
    /// </summary>
    public readonly float dt = dt;

    /// <summary>
    /// The grid that this device is currently on.
    /// </summary>
    public readonly Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? Grid = grid == null
        ? null
        : (grid.Value, grid.Value, grid.Value);

    /// <summary>
    /// The map that the device & grid is on.
    /// </summary>
    public readonly Entity<MapAtmosphereComponent?>? Map = map;
}
