using Content.Shared.FixedPoint;
using Robust.Shared.Physics.Components;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    public virtual void EnterDeadState(EntityUid uid)
    {
        EnsureComp<CollisionWakeComponent>(uid);
        _standing.Down(uid);

        if (_standing.IsDown(uid) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetCanCollide(physics, false);
        }

        _appearance.SetData(uid, DamageStateVisuals.State, DamageState.Dead);
    }

    public virtual void ExitDeadState(EntityUid uid)
    {
        RemComp<CollisionWakeComponent>(uid);

        _standing.Stand(uid);

        if (!_standing.IsDown(uid) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            _physics.SetCanCollide(physics, true);
        }
    }

    public virtual void UpdateDeadState(EntityUid entity, FixedPoint2 threshold) {}
}
