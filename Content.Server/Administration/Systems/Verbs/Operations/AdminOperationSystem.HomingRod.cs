using System.Numerics;
using Content.Server.Physics.Components;
using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Movement.Components;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnHomingRod(Entity<MetaDataComponent> entity, ref AdminOperationEvent<HomingRodOperation> args)
    {
        var speed = args.Operation.Speed;
        if (args.Operation.MatchTargetSprintSpeed &&
            TryComp<MovementSpeedModifierComponent>(entity, out var movement))
            speed = movement.CurrentSprintSpeed + 0.001f;

        // TODO: Reuse the immovable rod rule's spawning logic once it exposes a suitable API.
        IRobustRandom random = new RobustRandom();
        random.SetSeed(entity.Owner.Id);
        var offset = random.NextAngle().RotateVec(new Vector2(args.Operation.Distance, 0));
        var spawnCoords = _transform.GetMapCoordinates(entity).Offset(offset);
        var rod = Spawn(args.Operation.Prototype, spawnCoords);

        // TODO: ChasingWalkSystem needs an API for pinning a target and configuring its movement.
        // Remove AdminOperationSystem from ChasingWalkComponent's Access list once that exists.
        EnsureComp<ChasingWalkComponent>(rod, out var chasing);
        chasing.NextChangeVectorTime = TimeSpan.MaxValue;
        chasing.ChasingEntity = entity.Owner;
        chasing.ImpulseInterval = 0.1f;
        chasing.RotateWithImpulse = true;
        chasing.MaxSpeed = speed;
        chasing.Speed = speed;

        if (TryComp<TimedDespawnComponent>(rod, out var despawn))
            despawn.Lifetime = offset.Length() / speed * 3;
    }
}
