using Content.Client.Power.Components;

namespace Content.Client.Power.EntitySystems;

public static class StaticPowerSystem
{
    // Using this makes the call shorter.
    // ReSharper disable once UnusedParameter.Global
    public static bool IsPowered(this EntitySystem system, EntityUid uid, IEntityManager entManager, ApcPowerReceiverComponent? receiver = null)
    {
        if (receiver == null && !entManager.TryGetComponent(uid, out receiver))
            return false;

        return receiver.Powered;
    }
}
