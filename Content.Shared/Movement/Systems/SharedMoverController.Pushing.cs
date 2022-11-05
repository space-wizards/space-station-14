using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private bool _pushingEnabled;

    private void InitializePushing()
    {
        _configManager.OnValueChanged(CCVars.MobPushing, SetPushing, true);
    }

    private void SetPushing(bool value)
    {
        if (_pushingEnabled == value) return;

        _pushingEnabled = value;

        if (_pushingEnabled)
        {
            PhysicsSystem.KinematicControllerCollision += OnMobCollision;
        }
        else
        {
            PhysicsSystem.KinematicControllerCollision -= OnMobCollision;
        }
    }

    private void ShutdownPushing()
    {
        if (_pushingEnabled)
            PhysicsSystem.KinematicControllerCollision -= OnMobCollision;

        _configManager.UnsubValueChanged(CCVars.MobPushing, SetPushing);
    }

    /// <summary>
    ///     Fake pushing for player collisions.
    /// </summary>
    private void OnMobCollision(Fixture ourFixture, Fixture otherFixture, float frameTime, Vector2 worldNormal)
    {
        if (!_pushingEnabled)
            return;

        var otherBody = otherFixture.Body;

        if (otherBody.BodyType != BodyType.Dynamic || !otherFixture.Hard)
            return;

        if (!EntityManager.TryGetComponent(ourFixture.Body.Owner, out MobMoverComponent? mobMover) || worldNormal == Vector2.Zero)
            return;

        PhysicsSystem.ApplyLinearImpulse(otherBody, -worldNormal * mobMover.PushStrengthVV * frameTime);
    }
}
