using Content.Shared.DeviceNetwork.Components.Networks;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// An event raised when a <see cref="DeviceManagerRelatedComponent"/> is shutting down,
/// forcing the managers that control it to be deleted.
/// </summary>
[ByRefEvent]
public record struct DeviceManagerRelatedShutdownEvent(EntityUid Related);
