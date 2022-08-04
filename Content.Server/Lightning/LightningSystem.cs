using Content.Server.Lightning.Components;
using Content.Shared.Interaction;
using Content.Shared.Lightning;
using Content.Shared.Lightning.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, InteractHandEvent>(OnHandInteract);

        SubscribeLocalEvent<LightningComponent, LightningEvent>(OnLightning);
    }

    private void OnLightning(EntityUid uid, LightningComponent component, LightningEvent ev)
    {
        var offset = ev.Offset;
        var ent = Spawn("LightningBase", offset);
        var shape = new EdgeShape(ev.OffsetCorrection, new Vector2(0,0));
        var distanceLength = ev.OffsetCorrection.Length;
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics) &&
            TryComp<TransformComponent>(ent, out var xForm))
        {
            sprites.Rotation = ev.Angle;
            var fixture = new Fixture(physics, shape)
            {
                //TODO: Figure out what else should be added here, on impact doesn't shock but on collide does.
                ID = "LightningBody",
                Hard = false,
                CollisionMask = (int)CollisionGroup.ItemMask,
                CollisionLayer = (int)CollisionGroup.SlipLayer
            };

            _fixture.TryCreateFixture(physics, fixture);

            var entXForm = Transform(ent);

            entXForm.AttachParent(component.Owner);

            for (int i = 0; i < distanceLength-1; i++)
            {
                offset = offset.Offset(ev.CalculatedDistance.Normalized);
                var newEnt = Spawn("LightningBase", offset);
                if (!TryComp<SpriteComponent>(newEnt, out var newSprites))
                    return;
                newSprites.Rotation = ev.Angle;
                Transform(newEnt).AttachParent(ent);
            }
        }
    }

    private void OnHandInteract(EntityUid uid, LightningComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        //TODO: Get rid of all of this, it was just a test to see how this works with spawning.
        var compXForm = Transform(component.Owner);
        var compCoords = compXForm.Coordinates;
        var userXForm = Transform(args.User);
        var userCoords = userXForm.Coordinates;

        var calculatedDistance = userXForm.LocalPosition - compXForm.LocalPosition;
        var userAngle = calculatedDistance.ToWorldAngle(); //This plus the above distance works.

        var offset = compCoords.Offset(calculatedDistance.Normalized);
        var offsetCorrection = (calculatedDistance / calculatedDistance.Length) * (calculatedDistance.Length - 1);

        var ev = new LightningEvent(compCoords, userCoords, userAngle, component.MaxLength, calculatedDistance, offset, offsetCorrection);
        RaiseLocalEvent(uid, ev, true);

        args.Handled = true;
    }


}
