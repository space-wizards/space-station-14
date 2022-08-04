using Content.Server.Lightning.Components;
using Content.Shared.Interaction;
using Content.Shared.Lightning;
using Content.Shared.Lightning.Components;
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

        SubscribeAllEvent<LightningEvent>(OnLightning);
    }

    private void OnLightning(LightningEvent ev)
    {
        var ent = Spawn("LightningBase", ev.Offset);
        var shape = new EdgeShape(ev.OffsetCorrection, new Vector2(0,0));
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics) &&
            TryComp<TransformComponent>(ent, out var xForm))
        {
            //TODO: Scale doesn't work, try adding other entities without fixtures instead..
            sprites.Rotation = ev.Angle;
            var fixture = new Fixture(physics, shape)
            {
                //TODO: Figure out what else should be added here.
                ID = "LightningBody"
            };

            _fixture.TryCreateFixture(physics, fixture);

            //TODO: Spawn more entities of lightning prototype without fixtures to complete the chain and also parent them
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
