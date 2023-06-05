using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<LightningComponent, ComponentRemove>(OnRemove);
    }

    private void OnRemove(EntityUid uid, LightningComponent component, ComponentRemove args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam) || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        beamController.CreatedBeams.Remove(uid);
    }

    private void OnCollide(EntityUid uid, LightningComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam)
            || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
            return;

        if (!component.CanArc || beamController.CreatedBeams.Count >= component.MaxTotalArcs)
            return;

        Arc(component, args.OtherEntity, lightningBeam.VirtualBeamController.Value);

        if (component.ArcTarget == null)
            return;

        var spriteState = LightningRandomizer();
        component.ArcTargets.Add(args.OtherEntity);
        component.ArcTargets.Add(component.ArcTarget.Value);

        _beam.TryCreateBeam(
            args.OtherEntity,
            component.ArcTarget.Value,
            component.LightningPrototype,
            spriteState,
            controller: lightningBeam.VirtualBeamController.Value);
    }

    /// <summary>
    /// Fires lightning from user to target
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="target">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    public void ShootLightning(EntityUid user, EntityUid target, string lightningPrototype = "Lightning")
    {
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);
    }

    /// <summary>
    /// Looks for a target to arc to in all 8 directions, adds the closest to a local dictionary and picks at random
    /// </summary>
    /// <param name="component"></param>
    /// <param name="target"></param>
    /// <param name="controllerEntity"></param>
    private void Arc(LightningComponent component, EntityUid target, EntityUid controllerEntity)
    {
        if (!TryComp<BeamComponent>(controllerEntity, out var controller))
            return;

        var targetXForm = Transform(target);
        var directions = Enum.GetValues<Direction>().Length;

        var lightningQuery = GetEntityQuery<LightningComponent>();
        var beamQuery = GetEntityQuery<BeamComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        Dictionary<Direction, EntityUid> arcDirections =  new();

        //TODO: Add scoring system for the Tesla PR which will have grounding rods
        for (int i = 0; i < directions; i++)
        {
            var direction = (Direction) i;
            var (targetPos, targetRot) = targetXForm.GetWorldPositionRotation(xformQuery);
            var dirRad = direction.ToAngle() + targetRot;
            var ray = new CollisionRay(targetPos, dirRad.ToVec(), component.CollisionMask);
            var rayCastResults = _physics.IntersectRay(targetXForm.MapID, ray, component.MaxLength, target, false).ToList();

            RayCastResults? closestResult = null;

            foreach (var result in rayCastResults)
            {
                if (lightningQuery.HasComponent(result.HitEntity)
                    || beamQuery.HasComponent(result.HitEntity)
                    || component.ArcTargets.Contains(result.HitEntity)
                    || controller.HitTargets.Contains(result.HitEntity)
                    || controller.BeamShooter == result.HitEntity)
                {
                    continue;
                }

                closestResult = result;
                break;
            }

            if (closestResult == null)
            {
                continue;
            }

            arcDirections.Add(direction, closestResult.Value.HitEntity);
        }

        var randomDirection = (Direction) _random.Next(0, 7);

        if (arcDirections.ContainsKey(randomDirection))
        {
            component.ArcTarget = arcDirections.GetValueOrDefault(randomDirection);
            arcDirections.Clear();
        }
    }
}
