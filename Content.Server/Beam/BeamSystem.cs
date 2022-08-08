using Content.Server.Beam.Components;
using Content.Shared.Beam;
using Content.Shared.Beam.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Content.Server.Beam;

public sealed class BeamSystem : SharedBeamSystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeamComponent, BeamEvent>(OnBeam);
    }

    private void OnBeam(EntityUid uid, BeamComponent component, BeamEvent ev)
    {
        //CreateBeam(component, ev.Angle, ev.CalculatedDistance, ev.Offset, ev.OffsetCorrection);
    }

    /// <summary>
    /// Creates the beam.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="prototype"></param>
    /// <param name="userAngle"></param>
    /// <param name="calculatedDistance"></param>
    /// <param name="beamOffset"></param>
    /// <param name="offsetCorrection"></param>
    public void CreateBeam(EntityUid user, string prototype, Angle userAngle, Vector2 calculatedDistance, EntityCoordinates beamOffset, Vector2 offsetCorrection, string? bodyState = null)
    {
        var offset = beamOffset;
        var ent = Spawn(prototype, offset);
        var shape = new EdgeShape(offsetCorrection, new Vector2(0,0));
        var distanceLength = offsetCorrection.Length;
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics) &&
            TryComp<TransformComponent>(ent, out var xForm))
        {
            sprites.Rotation = userAngle;
            if (bodyState != null)
            {
                sprites.LayerSetState(0, bodyState);
                sprites.LayerSetShader(0, "unshaded");
            }
            var fixture = new Fixture(physics, shape)
            {
                ID = "BeamBody",
                Hard = false,
                Body = { BodyType = BodyType.Dynamic},
                CollisionMask = (int)CollisionGroup.ItemMask, //Change to MobMask
                CollisionLayer = (int)CollisionGroup.SlipLayer //Change to WallLayer
            };

            _fixture.TryCreateFixture(physics, fixture);

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
                    newSprites.LayerSetShader(0, "unshaded");
                }
                Transform(newEnt).AttachParent(ent);
            }
        }
    }

    /// <summary>
    /// Tries to create the beam
    /// </summary>
    /// <param name="user"></param>
    /// <param name="target"></param>
    /// <param name="bodyPrototype"></param>
    /// <param name="bodyState"></param>
    public void TryCreateBeam(EntityUid user, EntityUid target, string bodyPrototype, string? bodyState = null)
    {
        var compXForm = Transform(user);
        var compCoords = compXForm.Coordinates;
        var userXForm = Transform(target);

        var calculatedDistance = userXForm.LocalPosition - compXForm.LocalPosition;
        var userAngle = calculatedDistance.ToWorldAngle();

        var offset = compCoords.Offset(calculatedDistance.Normalized);

        //Don't divide by zero
        if (calculatedDistance.Length == 0)
            return;

        var offsetCorrection = (calculatedDistance / calculatedDistance.Length) * (calculatedDistance.Length - 1);

        CreateBeam(user, bodyPrototype, userAngle, calculatedDistance, offset, offsetCorrection, bodyState);
    }
}
