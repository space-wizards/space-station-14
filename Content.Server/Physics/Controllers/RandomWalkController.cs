using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Random;

using Content.Server.Physics.Components;
using Content.Shared.Throwing;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// The entity system responsible for managing <see cref="RandomWalkComponent"/>s.
/// Handles updating the direction they move in when their cooldown elapses.
/// </summary>
internal sealed class RandomWalkController : VirtualController
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Updates the cooldowns of all random walkers.
    /// If each of them is off cooldown it updates their velocity and resets its cooldown.
    /// </summary>
    /// <param name="prediction">??? Not documented anywhere I can see ???</param> // TODO: Document this.
    /// <param name="frameTime">The amount of time that has elapsed since the last time random walk cooldowns were updated.</param>
    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        foreach(var (randomWalk, physics) in EntityManager.EntityQuery<RandomWalkComponent, PhysicsComponent>())
        {
            if (EntityManager.HasComponent<ActorComponent>(randomWalk.Owner)
            ||  EntityManager.HasComponent<ThrownItemComponent>(randomWalk.Owner))
                continue;

            if((randomWalk._timeUntilNextStep -= frameTime) > 0f)
                continue;

            Update(randomWalk, physics);
        }
    }

    /// <summary>
    /// Updates the direction and speed a random walker is moving at.
    /// Also resets the random walker's cooldown.
    /// </summary>
    /// <param name="randomWalk">The random walker state.</param>
    /// <param name="physics">The physics body associated with the random walker.</param>
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
