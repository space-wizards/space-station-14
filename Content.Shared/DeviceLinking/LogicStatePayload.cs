using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

public sealed partial class LogicStatePayload : NetworkPayloadBase<LogicStatePayload>
{
    [DataField]
    public SignalState State;
}
