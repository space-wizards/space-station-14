using Robust.Shared.Serialization;

namespace Content.Shared.Power.Components;

[Serializable, NetSerializable]
public sealed class PowerReceiverComponentState : ComponentState
{
    public bool Powered;
    public bool NeedsPower;
    public bool Enabled;
}
