using Content.Shared.FixedPoint;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{
    public virtual void EnterDeadState(EntityUid uid)
    {
        EnsureComp<CollisionWakeComponent>(uid);
        Standing.Down(uid);

        if (Standing.IsDown(uid) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            physics.CanCollide = false;
        }

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
        }
    }

    public virtual void ExitDeadState(EntityUid uid)
    {
        RemComp<CollisionWakeComponent>(uid);

        Standing.Stand(uid);

        if (!Standing.IsDown(uid) && TryComp<PhysicsComponent>(uid, out var physics))
        {
            physics.CanCollide = true;
        }
    }

    public virtual void UpdateDeadState(EntityUid entity, FixedPoint2 threshold) {}
}
