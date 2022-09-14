using Content.Shared.StatusEffect;

namespace Content.Server.MobState;

public sealed partial class MobStateSystem
{
    public override void EnterCritState(EntityUid uid)
    {
        base.EnterCritState(uid);

        if (HasComp<StatusEffectsComponent>(uid))
        {
            Status.TryRemoveStatusEffect(uid, "Stun");
        }
    }
}
