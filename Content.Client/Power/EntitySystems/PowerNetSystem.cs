using Content.Shared.Power.Components;
using Content.Shared.Power.Systems;

namespace Content.Client.Power.EntitySystems;

public sealed class PowerNetSystem : SharedPowerNetSystem
{
    public override bool IsPoweredCalculate(PowerReceiverComponent comp)
    {
        return comp.Enabled && !comp.NeedsPower;
    }
}
