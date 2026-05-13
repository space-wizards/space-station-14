using Robust.Shared.Serialization;

namespace Content.Shared.Power.Components;

[Serializable, NetSerializable]
public sealed partial class ApcPowerReceiverComponentState : ComponentState
{
    public bool Powered;
    public bool NeedsPower;
    public bool PowerDisabled;
}

