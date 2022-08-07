using System.Linq;
using Content.Server.Beam;
using Content.Server.Construction.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BeamSystem _beam = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, LightningComponent component, StartCollideEvent args)
    {
        if (component.MaxArc > 0 && component.Counter < component.MaxArc)
        {
            Arc(component, component.Owner, args.OtherFixture.Body.Owner);
            _beam.TryCreateBeam(args.OtherFixture.Body.Owner, component.ArcTarget, "LightningBase");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    //TODO: Currently has the potential to spawn off map in NaN and potentially infinte loop.
    public void Arc(LightningComponent component, EntityUid user, EntityUid target)
    {
        var targetXForm = Transform(target);

        //TODO: Raycast works in all directions but might want to change how it works
        var directions = Enum.GetValues<Direction>().Length;
        for (int i = 0; i < directions; i++)
        {
            var direction = (Direction)i;
            var dirRad = direction.ToAngle() + targetXForm.GetWorldPositionRotation().WorldRotation;
            var ray = new CollisionRay(targetXForm.GetWorldPositionRotation().WorldPosition, dirRad.ToVec(), (int)CollisionGroup.ItemMask);
            var rayCastResults = _physics.IntersectRay(targetXForm.MapID, ray, component.MaxLength, target, false).ToList();
            var lightningQuery = GetEntityQuery<LightningComponent>();
            var machineQuery = GetEntityQuery<MachineComponent>();

            RayCastResults? closestResult = null;

            foreach (var result in rayCastResults)
            {
                if (lightningQuery.HasComponent(result.HitEntity) || machineQuery.HasComponent(result.HitEntity))
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
