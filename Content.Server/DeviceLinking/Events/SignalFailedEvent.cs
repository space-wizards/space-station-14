using Content.Server.DeviceNetwork;
using Content.Shared.DeviceNetwork;

namespace Content.Server.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct SignalFailedEvent(EntityUid? uid, bool failed);
