using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// Contains a logic state of a <see cref="SignalPayload"/>.
/// </summary>
public sealed partial class LogicStatePayload : NetworkPayloadBase<LogicStatePayload>
{
    [DataField]
    public SignalState State;
}
