using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed partial class LogicStatePayload : NetworkPayloadBase<LogicStatePayload>
{
    [DataField]
    public SignalState State;
}
