namespace Content.Shared.Atmos.Piping.Components;

/// <summary>
/// <para>Raised directed on an AtmosDeviceComponent when it has been removed from
/// the GridAtmosphereComponent it was attached to.
/// This can occur when it has been requested manually or when the device has been unanchored.</para>
///
/// <para>Any information that you were tracking about the grid should
/// probably be cleared out here.</para>
/// </summary>
[ByRefEvent]
public readonly record struct AtmosDeviceDisabledEvent;
