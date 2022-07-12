using Content.Shared.Alert;
using Content.Shared.FixedPoint;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    public virtual void EnterCritState(EntityUid uid)
    {
        Alerts.ShowAlert(uid, AlertType.HumanCrit);

        Standing.Down(uid);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
        }
    }

    public virtual void ExitCritState(EntityUid uid)
    {
        Standing.Stand(uid);
    }

    public virtual void UpdateCritState(EntityUid entity, FixedPoint2 threshold) {}
}
