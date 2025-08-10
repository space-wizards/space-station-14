using Content.Shared._Starlight.Power.Components;

namespace Content.Shared._Starlight.Power.EntitySystems;

public sealed class PoweredLockerSystem : EntitySystem
{
    public void TogglePower(EntityUid uid, PoweredLockerComponent? powerComp = null, bool? powered = null)
    {
        if (!Resolve(uid, ref powerComp))
            return;
        
        if (powered == null)
            powered = !powerComp.Powered;

        powerComp.Powered = powered.Value;
        Dirty(uid, powerComp);
    }
}