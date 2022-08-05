using System.Linq;
using Content.Server.Containers;
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

    //TODO: Make this a CreateLightning event or something
    private void OnLightning(EntityUid uid, LightningComponent component, LightningEvent ev)
    {
        CreateLightning(component, ev.Angle, ev.CalculatedDistance, ev.Offset, ev.OffsetCorrection);
    }

    /// <summary>
    /// Called where the target data spawns lightning from user to target
    /// </summary>
    /// <param name="component"></param>
    /// <param name="userAngle"></param>
    /// <param name="calculatedDistance"></param>
    /// <param name="lightningOffset"></param>
    /// <param name="offsetCorrection"></param>
    public void CreateLightning(LightningComponent component, Angle userAngle, Vector2 calculatedDistance, EntityCoordinates lightningOffset, Vector2 offsetCorrection)
    {
        var offset = lightningOffset;
        var ent = Spawn(component.BodyPrototype, offset);
        var shape = new EdgeShape(offsetCorrection, new Vector2(0,0));
        var distanceLength = offsetCorrection.Length;
        if (TryComp<SpriteComponent>(ent, out var sprites) && TryComp<PhysicsComponent>(ent, out var physics) &&
            TryComp<TransformComponent>(ent, out var xForm))
        {
            sprites.Rotation = userAngle;
            var fixture = new Fixture(physics, shape)
            {
                ID = "LightningBody",
                Hard = false,
                Body = { BodyType = BodyType.Dynamic},
                CollisionMask = (int)CollisionGroup.ItemMask,
                CollisionLayer = (int)CollisionGroup.SlipLayer
            };

            _fixture.TryCreateFixture(physics, fixture);

            var entXForm = Transform(ent);

            entXForm.AttachParent(component.Owner);

            for (int i = 0; i < distanceLength-1; i++)
            {
                offset = offset.Offset(calculatedDistance.Normalized);
                var newEnt = Spawn(component.BodyPrototype, offset);
                if (!TryComp<SpriteComponent>(newEnt, out var newSprites))
                    return;
                newSprites.Rotation = userAngle;
                Transform(newEnt).AttachParent(ent);
            }
        }
    }

    /// <summary>
    /// Gets the Target Data for the lightning
    /// </summary>
    /// <param name="component"></param>
    /// <param name="target"></param>
    public void GetTargetData(LightningComponent component, EntityUid target)
    {
        var compXForm = Transform(component.Owner);
        var compCoords = compXForm.Coordinates;
        var userXForm = Transform(target);

        var calculatedDistance = userXForm.LocalPosition - compXForm.LocalPosition;
        var userAngle = calculatedDistance.ToWorldAngle();

        var offset = compCoords.Offset(calculatedDistance.Normalized);
        var offsetCorrection = (calculatedDistance / calculatedDistance.Length) * (calculatedDistance.Length - 1);

        var ev = new LightningEvent(userAngle, calculatedDistance, offset, offsetCorrection);
        RaiseLocalEvent(component.Owner, ev, true);

        if (component.MaxArc > 0 && !component.ArcTargets.Contains(component.Owner) || !component.ArcTargets.Contains(target))
        {
            component.ArcTargets.Add(component.Owner);
            component.ArcTargets.Add(target);
            Arc(component, target);
        }
    }

    public void Arc(LightningComponent component, EntityUid target)
    {
        if (HasComp<LightningComponent>(target) || target.ContainsPrototypeRecursive(component.BodyPrototype))
        {
            return;
        }
    }

    private void OnHandInteract(EntityUid uid, LightningComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        GetTargetData(component, args.User);

        args.Handled = true;
    }


}
