using Content.Shared.Alert;
using Content.Shared.FixedPoint;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    public virtual void EnterCritState(EntityUid uid)
    {
        Alerts.ShowAlert(uid, AlertType.HumanCrit);
        _standing.Down(uid);
        _appearance.SetData(uid, DamageStateVisuals.State, DamageState.Critical);
    }

    public virtual void ExitCritState(EntityUid uid)
    {
        _standing.Stand(uid);
    }

    public virtual void UpdateCritState(EntityUid entity, FixedPoint2 threshold) {}
}
