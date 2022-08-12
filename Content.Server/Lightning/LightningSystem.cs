using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Construction.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
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
        SubscribeLocalEvent<LightningComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnShutdown(EntityUid uid, LightningComponent component, ComponentShutdown args)
    {
        //throw new NotImplementedException();
    }

    private void OnCollide(EntityUid uid, LightningComponent component, StartCollideEvent args)
    {
        if (component.MaxArc > 0 && component.Counter < component.MaxArc)
        {
            if (!TryComp<BeamComponent>(uid, out var lightningBeam) || lightningBeam.VirtualBeamController == null)
                return;

            Arc(component, args.OtherFixture.Body.Owner, lightningBeam.VirtualBeamController.Value);

            //When the lightning is made with TryCreateBeam, spawns random sprites for each beam to make it look nicer.
            var spriteStateNumber = _random.Next(1, 12);
            var spriteState = ("lightning_" + spriteStateNumber);

            component.ArcTargets.Add(args.OtherFixture.Body.Owner);
            component.ArcTargets.Add(component.ArcTarget);

            _beam.TryCreateBeam(args.OtherFixture.Body.Owner, component.ArcTarget, "LightningBase", spriteState, controller: lightningBeam.VirtualBeamController.Value);
        }
    }

    public void Arc(LightningComponent component, EntityUid target, EntityUid contollerEntity)
    {
        if (!TryComp<BeamComponent>(contollerEntity, out var controller))
            return;

        var targetXForm = Transform(target);
        var directions = Enum.GetValues<Direction>().Length;

        var lightningQuery = GetEntityQuery<LightningComponent>();
        var beamQuery = GetEntityQuery<BeamComponent>();

        for (int i = 0; i < directions; i++)
        {
            var direction = (Direction) i;
            var dirRad = direction.ToAngle() + targetXForm.GetWorldPositionRotation().WorldRotation;
            var ray = new CollisionRay(targetXForm.GetWorldPositionRotation().WorldPosition, dirRad.ToVec(),
                component.CollisionMask);
            var rayCastResults = _physics.IntersectRay(targetXForm.MapID, ray, component.MaxLength, target, false)
                .ToList();

            RayCastResults? closestResult = null;

            foreach (var result in rayCastResults)
            {
                if (lightningQuery.HasComponent(result.HitEntity) || beamQuery.HasComponent(result.HitEntity) ||
                    component.ArcTargets.Contains(result.HitEntity) || controller.HitTargets.Contains(result.HitEntity))
                {
                    continue;
                }

                closestResult = result;
            }

            if (closestResult == null)
            {
                continue;
            }

            component.ArcTarget = closestResult.Value.HitEntity;
            component.Counter++;
        }
    }
}
