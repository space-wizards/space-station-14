using Content.Shared.FixedPoint;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    public virtual void EnterNormState(EntityUid uid)
    {
        _standing.Stand(uid);
        _appearance.SetData(uid, DamageStateVisuals.State, DamageState.Alive);
    }

    public virtual void UpdateNormState(EntityUid entity, FixedPoint2 threshold) {}

    public virtual void ExitNormState(EntityUid uid) {}
}
