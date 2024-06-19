using System.Linq;
using System.Numerics;
using Content.Server.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// A system which makes its entity chasing another entity with selected component.
/// </summary>
public sealed class ChasingWalkSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly HashSet<Entity<IComponent>> _potentialChaseTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasingWalkComponent, MapInitEvent>(OnChasingMapInit);
    }

    private void OnChasingMapInit(EntityUid uid, ChasingWalkComponent component, MapInitEvent args)
    {
        component.NextImpulseTime = _gameTiming.CurTime;
        component.NextChangeVectorTime = _gameTiming.CurTime;
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ChasingWalkComponent>();
        while (query.MoveNext(out var uid, out var chasing))
        {
            //Set Velocity to Target
            if (chasing.NextImpulseTime <= _gameTiming.CurTime)
            {
                ForceImpulse(uid, chasing);
                chasing.NextImpulseTime += TimeSpan.FromSeconds(chasing.ImpulseInterval);
            }
            //Change Target
            if (chasing.NextChangeVectorTime <= _gameTiming.CurTime)
            {
                ChangeTarget(uid, chasing);

                var delay = TimeSpan.FromSeconds(_random.NextFloat(chasing.ChangeVectorMinInterval, chasing.ChangeVectorMaxInterval));
                chasing.NextChangeVectorTime += delay;
            }
        }
    }

    private void ChangeTarget(EntityUid uid, ChasingWalkComponent component)
    {
        //We find our coordinates and calculate the radius of the target search.
        var xform = Transform(uid);
        var range = component.MaxChaseRadius;
        var compType = _random.Pick(component.ChasingComponent.Values).Component.GetType();
        _potentialChaseTargets.Clear();
        _lookup.GetEntitiesInRange(compType, _transform.GetMapCoordinates(xform), range, _potentialChaseTargets, LookupFlags.Uncontained);

        //If there are no required components in the radius, don't moving.
        if (_potentialChaseTargets.Count <= 0)
            return;

        //In the case of finding required components, we choose a random one of them and remember its uid.
        component.ChasingEntity = _random.Pick(_potentialChaseTargets).Owner;
        component.Speed = _random.NextFloat(component.MinSpeed, component.MaxSpeed);
    }

    //pushing the entity toward its target
    private void ForceImpulse(EntityUid uid, ChasingWalkComponent component)
    {
        if (Deleted(component.ChasingEntity) || component.ChasingEntity == null)
        {
            ChangeTarget(uid, component);
            return;
        }

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        //Calculating direction to the target.
        var pos1 = _transform.GetWorldPosition(uid);
        var pos2 = _transform.GetWorldPosition(component.ChasingEntity.Value);

        var delta = pos2 - pos1;
        var speed = delta.Length() > 0 ? delta.Normalized() * component.Speed : Vector2.Zero;

        _physics.SetLinearVelocity(uid, speed);
        _physics.SetBodyStatus(uid, physics, BodyStatus.InAir); //If this is not done, from the explosion up close, the tesla will "Fall" to the ground, and almost stop moving.
    }
}
