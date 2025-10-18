using Content.Client.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Client.Power.EntitySystems;

public sealed class PowerNetSystem : SharedPowerNetSystem
{
    public override bool IsPoweredCalculate(SharedApcPowerReceiverComponent comp)
    {
        return IsPoweredCalculate((ApcPowerReceiverComponent)comp);
    }

    private bool IsPoweredCalculate(ApcPowerReceiverComponent comp)
    {
        return !comp.PowerDisabled
               && !comp.NeedsPower;
    }
}
