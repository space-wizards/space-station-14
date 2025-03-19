using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

public static class StaticPowerSystem
{
    // Using this makes the call shorter.
    // ReSharper disable once UnusedParameter.Global
    public static bool IsPowered(this EntitySystem system,
        EntityUid uid,
        IEntityManager entManager,
        ApcPowerReceiverComponent? receiver = null,
        PowerNetworkBatteryComponent? battery = null)
    {
        if (receiver != null || entManager.TryGetComponent(uid, out receiver))
            return receiver.Powered;

        if (battery != null || entManager.TryGetComponent(uid, out battery))
            return battery.Enabled;
        
        return true;
    }
}
