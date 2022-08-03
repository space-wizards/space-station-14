using Content.Server.Lightning.Components;
using Content.Shared.Interaction;
using Content.Shared.Lightning;
using Content.Shared.Lightning.Components;
using Robust.Server.GameObjects;
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
        //TODO: Need to spawn an offset from the comp owner coords, not on them directly.
        var ent = Spawn("LightningBase", ev.OwnerCoords.Offset(ev.OwnerCoords.Position.ToAngle().ToWorldVec().Normalized));
        //TODO: Edge shape works but the position is wacky as hell if done like this (like miles away from the lightning)
        var shape = new EdgeShape(ev.OwnerCoords.Position, ev.TargetCoords.Position);
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics) &&
            TryComp<TransformComponent>(ent, out var xForm))
        {
            //TODO: Just X makes it too fat, X and Y with the same distance makes it too large, find the correct Y to work for this.
            //Defintely have more y than x here. 1-1.5 seems to work out perfectly without it looking too fat
            //Doesn't really feel like lightning yet though.
            sprites.Scale = new Vector2(1.5f, 3f);
            sprites.Rotation = ev.Angle;
            var fixture = new Fixture(physics, shape)
            {
                //TODO: Figure out what else should be added here.
                ID = "LightningBody"
            };

            _fixture.TryCreateFixture(physics, fixture);
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

        var distance = userXForm.LocalPosition - compXForm.LocalPosition;
        var userAngle = distance.ToWorldAngle(); //This plus the above distance works.

        var ev = new LightningEvent(compCoords, userCoords, userAngle, component.MaxLength, distance);
        RaiseLocalEvent(uid, ev, true);

        args.Handled = true;
    }


}
