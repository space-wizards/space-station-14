using Content.Shared.Beam;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Beam;

public sealed class BeamSystem : SharedBeamSystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// If <see cref="TryCreateBeam"/> is successful, it spawns a beam from the user to the target.
    /// </summary>
    /// <param name="prototype">The prototype used to make the beam</param>
    /// <param name="userAngle">Angle of the user firing the beam</param>
    /// <param name="calculatedDistance">The calculated distance from the user to the target.</param>
    /// <param name="beamOffset">The offset of the beam from the user.</param>
    /// <param name="offsetCorrection">Calculated offset correction so the <see cref="EdgeShape"/> can be properly dynamically created</param>
    /// <param name="bodyState">Optional sprite state for the <see cref="prototype"/> if it needs a dynamic one</param>
    /// <param name="shader">Optional shader for the <see cref="prototype"/> and <see cref="bodyState"/> if it needs something other than default</param>
    private void CreateBeam(
        string prototype,
        Angle userAngle,
        Vector2 calculatedDistance,
        EntityCoordinates beamOffset,
        Vector2 offsetCorrection,
        string? bodyState = null,
        string shader = "unshaded")
    {
        var offset = beamOffset;
        var ent = Spawn(prototype, offset);
        var shape = new EdgeShape(offsetCorrection, new Vector2(0,0));
        var distanceLength = offsetCorrection.Length;
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics))
        {
            sprites.Rotation = userAngle;
            if (bodyState != null)
            {
                sprites.LayerSetState(0, bodyState);
                sprites.LayerSetShader(0, shader);
            }
            var fixture = new Fixture(physics, shape)
            {
                ID = "BeamBody",
                Hard = false,
                Body = { BodyType = BodyType.Dynamic},
                CollisionMask = (int)CollisionGroup.ItemMask, //Change to MobMask
                CollisionLayer = (int)CollisionGroup.MobLayer //Change to WallLayer
            };

            _fixture.TryCreateFixture(physics, fixture);

            Dirty(ent);

            //TODO: Sometime in the future this needs to be replaced with tiled sprites because lord this is shit.
            for (int i = 0; i < distanceLength-1; i++)
            {
                offset = offset.Offset(calculatedDistance.Normalized);
                var newEnt = Spawn(prototype, offset);
                if (!TryComp<SpriteComponent>(newEnt, out var newSprites))
                    return;
                newSprites.Rotation = userAngle;
                if (bodyState != null)
                {
                    newSprites.LayerSetState(0, bodyState);
                    newSprites.LayerSetShader(0, shader);
                }

                Transform(newEnt).AttachParent(ent);

                Dirty(newEnt);
            }
        }

    }

    /// <summary>
    /// Called where you want an entity to create a beam from one target to another.
    /// Tries to create the beam and does calculations like the distance, angle, and offset.
    /// </summary>
    /// <param name="user">The entity that's firing off the beam</param>
    /// <param name="target">The entity that's being targeted by the user</param>
    /// <param name="bodyPrototype">The prototype spawned when this beam is created</param>
    /// <param name="bodyState">Optional sprite state for the <see cref="bodyPrototype"/> if a default one is not given</param>
    /// <param name="shader">Optional shader for the <see cref="bodyPrototype"/> if a default one is not given</param>
    public void TryCreateBeam(EntityUid user, EntityUid target, string bodyPrototype, string? bodyState = null, string shader = "unshaded")
    {
        var userXForm = Transform(user);
        var targetXForm = Transform(target);

        var calculatedDistance = targetXForm.LocalPosition - userXForm.LocalPosition;
        var userAngle = calculatedDistance.ToWorldAngle();

        var offset = userXForm.Coordinates.Offset(calculatedDistance.Normalized);

        //Don't divide by zero
        if (calculatedDistance.Length == 0)
            return;

        var offsetCorrection = (calculatedDistance / calculatedDistance.Length) * (calculatedDistance.Length - 1);

        CreateBeam(bodyPrototype, userAngle, calculatedDistance, offset, offsetCorrection, bodyState, shader);
    }
}
