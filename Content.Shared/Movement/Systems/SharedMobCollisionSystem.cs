using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedMobCollisionSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<MobCollisionMessage>(OnCollision);
        SubscribeLocalEvent<MobCollisionComponent, ComponentStartup>(OnCollisionStartup);
    }

    private void OnCollisionStartup(Entity<MobCollisionComponent> ent, ref ComponentStartup args)
    {
        _fixtures.TryCreateFixture(ent.Owner,
            ent.Comp.Shape,
            "mob_collision",
            hard: false,
            collisionLayer: (int) CollisionGroup.MidImpassable,
            collisionMask: (int) CollisionGroup.MidImpassable);
    }

    private void OnCollision(MobCollisionMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!HasComp<MobCollisionComponent>(player) || !TryComp(player.Value, out TransformComponent? xform))
            return;

        // TODO: Validation
        _xformSystem.SetLocalPosition(player.Value, xform.LocalPosition + msg.Direction);
    }

    [Serializable, NetSerializable]
    protected sealed class MobCollisionMessage : EntityEventArgs
    {
        public Vector2 Direction;
    }
}
