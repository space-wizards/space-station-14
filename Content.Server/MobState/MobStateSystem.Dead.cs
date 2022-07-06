using Content.Shared.Alert;
using Content.Shared.StatusEffect;

namespace Content.Server.MobState;

public sealed partial class MobStateSystem
{
    public override void EnterDeadState(EntityUid uid)
    {
        base.EnterDeadState(uid);

        Alerts.ShowAlert(uid, AlertType.HumanDead);

        if (HasComp<StatusEffectsComponent>(uid))
        {
            Status.TryRemoveStatusEffect(uid, "Stun");
        }
    }
}
