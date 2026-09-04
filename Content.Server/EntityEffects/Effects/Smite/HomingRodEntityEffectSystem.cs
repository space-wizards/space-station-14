using System.Numerics;
using Content.Server.Physics.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class HomingRodEntityEffectSystem : EntityEffectSystem<MetaDataComponent, HomingRod>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<HomingRod> args)
    {
        var speed = args.Effect.Speed;
        if (args.Effect.MatchTargetSprintSpeed &&
            TryComp<MovementSpeedModifierComponent>(entity, out var movement))
            speed = movement.CurrentSprintSpeed + 0.001f;

        IRobustRandom random = new RobustRandom();
        random.SetSeed(entity.Owner.Id);
        var offset = random.NextAngle().RotateVec(new Vector2(args.Effect.Distance, 0));
        var spawnCoords = _transform.GetMapCoordinates(entity).Offset(offset);
        var rod = Spawn(args.Effect.Prototype, spawnCoords);

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

public sealed partial class HomingRod : EntityEffectBase<HomingRod>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField(required: true)]
    public float Distance;

    [DataField(required: true)]
    public float Speed;

    [DataField]
    public bool MatchTargetSprintSpeed;
}
