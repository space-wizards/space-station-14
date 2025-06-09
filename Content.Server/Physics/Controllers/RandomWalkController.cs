using System.Numerics;
using Content.Server.Physics.Components;
using Content.Server.Projectiles; // imp
using Content.Shared.Follower.Components;
using Content.Shared.Movement.Pulling.Components; // imp
using Content.Shared.Movement.Pulling.Systems; // imp
using Content.Shared.Projectiles; // imp
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// The entity system responsible for managing <see cref="RandomWalkComponent"/>s.
/// Handles updating the direction they move in when their cooldown elapses.
/// </summary>
internal sealed class RandomWalkController : VirtualController
{
    #region Dependencies
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ProjectileSystem _projectile = default!; // imp
    [Dependency] private readonly PullingSystem _pulling = default!; // imp
    [Dependency] private readonly TransformSystem _transform = default!; // imp
    [Dependency] private readonly ThrowingSystem _throwing = default!; // imp
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomWalkComponent, ComponentStartup>(OnRandomWalkStartup);
    }

    /// <summary>
    /// Updates the cooldowns of all random walkers.
    /// If each of them is off cooldown it updates their velocity and resets its cooldown.
    /// </summary>
    /// <param name="prediction">??? Not documented anywhere I can see ???</param> // TODO: Document this.
    /// <param name="frameTime">The amount of time that has elapsed since the last time random walk cooldowns were updated.</param>
    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<RandomWalkComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var randomWalk, out var physics))
        {
            if (EntityManager.HasComponent<ActorComponent>(uid)
            ||  EntityManager.HasComponent<ThrownItemComponent>(uid)
            ||  EntityManager.HasComponent<FollowerComponent>(uid))
                continue;

            var curTime = _timing.CurTime;
            if (randomWalk.NextStepTime <= curTime)
                Update(uid, randomWalk, physics);
        }
    }

    /// <summary>
    /// Updates the direction and speed a random walker is moving at.
    /// Also resets the random walker's cooldown.
    /// </summary>
    /// <param name="randomWalk">The random walker state.</param>
    /// <param name="physics">The physics body associated with the random walker.</param>
    public void Update(EntityUid uid, RandomWalkComponent? randomWalk = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref randomWalk))
            return;

        var curTime = _timing.CurTime;
        randomWalk.NextStepTime = curTime + TimeSpan.FromSeconds(_random.NextDouble(randomWalk.MinStepCooldown.TotalSeconds, randomWalk.MaxStepCooldown.TotalSeconds));
        if (!Resolve(uid, ref physics))
            return;

        var pushVec = _random.NextAngle().ToVec();
        pushVec += randomWalk.BiasVector;
        pushVec.Normalize();
        if (randomWalk.ResetBiasOnWalk)
            randomWalk.BiasVector *= 0f;
        var pushStrength = _random.NextFloat(randomWalk.MinSpeed, randomWalk.MaxSpeed);

        if (randomWalk.BreakPulling && TryComp<PullableComponent>(uid, out var pulling))
            _pulling.TryStopPull(uid, pulling);

        if (TryComp<EmbeddableProjectileComponent>(uid, out var embeddable) && embeddable.Target != null) // imp - everything after this is ours.
        {
            // calculate the direction away from the embed target
            var pos = _transform.GetWorldPosition(uid);
            var posTarget = _transform.GetWorldPosition(embeddable.Target.Value);
            var delta = posTarget - pos;
            var speed = delta.Length() > 0 ? delta.Normalized() * -1 : Vector2.Zero;

            _projectile.EmbedDetach(uid, embeddable);
            _physics.SetLinearVelocity(uid, physics.LinearVelocity * randomWalk.AccumulatorRatio + speed * pushStrength, body: physics);
        }
        else if (!randomWalk.Throw)
            _physics.SetLinearVelocity(uid, physics.LinearVelocity * randomWalk.AccumulatorRatio + pushVec * pushStrength, body: physics);
        else
        {
            _throwing.TryThrow(uid, physics.LinearVelocity * randomWalk.AccumulatorRatio + pushVec * pushStrength);
        }
    }

    /// <summary>
    /// Syncs up a random walker step timing when the component starts up.
    /// </summary>
    /// <param name="uid">The uid of the random walker to start up.</param>
    /// <param name="comp">The state of the random walker to start up.</param>
    /// <param name="args">The startup prompt arguments.</param>
    private void OnRandomWalkStartup(EntityUid uid, RandomWalkComponent comp, ComponentStartup args)
    {
        if (comp.StepOnStartup)
            Update(uid, comp);
        else
            comp.NextStepTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextDouble(comp.MinStepCooldown.TotalSeconds, comp.MaxStepCooldown.TotalSeconds));
    }
}
