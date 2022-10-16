using Content.Server.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Random;

using Content.Server.Physics.Components;

namespace Content.Server.Physics.Controllers;
internal sealed class RandomWalkController : VirtualController
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        foreach(var (randomWalk, physics) in EntityManager.EntityQuery<RandomWalkComponent, PhysicsComponent>())
        {
            if (EntityManager.HasComponent<ActorComponent>(randomWalk.Owner))
                continue;

            if((randomWalk._timeUntilNextStep -= frameTime) > 0f)
                continue;

            Update(randomWalk, physics);
        }
    }

    public void Update(RandomWalkComponent randomWalk, PhysicsComponent? physics = null)
    {
        randomWalk._timeUntilNextStep += _random.NextFloat(randomWalk.MinStepCooldown, randomWalk.MaxStepCooldown);
        if(!Resolve(randomWalk.Owner, ref physics))
            return;

        var pushAngle = _random.NextAngle();
        var pushStrength = _random.NextFloat(randomWalk.MinSpeed, randomWalk.MaxSpeed);

        _physics.SetLinearVelocity(physics, physics.LinearVelocity * randomWalk.AccumulatorRatio);
        _physics.ApplyLinearImpulse(physics, pushAngle.ToVec() * (pushStrength * physics.Mass));
    }
}
