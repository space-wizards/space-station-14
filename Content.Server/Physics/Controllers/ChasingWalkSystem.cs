using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Timing;

using Content.Server.Physics.Components;
using Content.Shared.Follower.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// A system which makes its entity chasing another entity with selected component.
/// </summary>
internal sealed class ChasingWalkSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;


    private EntityQuery<TransformComponent> _xformQuery;
    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChasingWalkComponent>();
        while (query.MoveNext(out var uid, out var chasing))
        {
            //Set Velocity to Target
            if (chasing.NextImpulseTime <= _gameTiming.CurTime)
            {
                ForceImpulse(uid, chasing);
                chasing.NextImpulseTime = _gameTiming.CurTime + TimeSpan.FromSeconds(chasing.ImpulseInterval);
            }
            //Change Target Part
            if (chasing.NextChangeVectorTime <= _gameTiming.CurTime)
            {
                ChangeTarget(uid, chasing);

                var delay = TimeSpan.FromSeconds(_random.NextFloat(chasing.ChangeVectorMinInterval, chasing.ChangeVectorMaxInterval));
                chasing.NextChangeVectorTime = _gameTiming.CurTime + delay;
            }
        }
    }
    private void ChangeTarget(EntityUid uid, ChasingWalkComponent component)
    {
        //We find our coordinates and calculate the radius of the target search.
        var xform = _xformQuery.GetComponent(uid);
        var range = component.MaxChaseRadius;
        var compType = EntityManager.ComponentFactory.GetRegistration(component.ChasingComponent).Type;
        var allEnts = _lookup.GetComponentsInRange(compType, xform.MapPosition, range)
            .Select(x => x.Owner).ToList();

        //If there are no required components in the radius, don't moving.
        if (allEnts.Count <= 0) return;

        //In the case of finding required components, we choose a random one of them and remember its uid.
        component.ChasingEntity = _random.Pick(allEnts);
        component.Speed = _random.NextFloat(component.MinSpeed, component.MaxSpeed);
    }

    //pushing the entity toward its target
    private void ForceImpulse(EntityUid uid, ChasingWalkComponent component)
    {
        if (!Initialized(uid))
            return;

        if (Deleted(component.ChasingEntity) || component.ChasingEntity == null)
        {
            ChangeTarget(uid, component);
            return;
        }

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        //Calculating direction to the target.
        var xform = _xformQuery.GetComponent(component.ChasingEntity.Value);
        var delta = xform.MapPosition.Position - Transform(uid).MapPosition.Position;

        //Changing the direction of the entity.
        var speed = delta.Normalized() * component.Speed;
        _physics.SetLinearVelocity(uid, speed);

        _physics.SetBodyStatus(physics, BodyStatus.InAir); //If this is not done, from the explosion up close, the tesla will "Fall" to the ground, and almost stop moving.
    }
}
