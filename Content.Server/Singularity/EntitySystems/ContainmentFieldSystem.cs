using Content.Server.Singularity.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems;

public class ContainmentFieldSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
    }

    private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, StartCollideEvent args)
    {
        var otherBody = args.OtherFixture.Body.Owner;

        if (TryComp<PhysicsComponent>(otherBody, out var physics) && physics.BodyType == BodyType.KinematicController)
        {
            var fieldDir = Transform(component.Owner).WorldPosition;
            var playerDir = Transform(otherBody).WorldPosition;

            _throwing.TryThrow(otherBody, playerDir-fieldDir, strength: component.ThrowForce);
        }
    }
}
