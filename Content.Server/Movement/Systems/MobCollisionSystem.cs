using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Player;

namespace Content.Server.Movement.Systems;

public sealed class MobCollisionSystem : SharedMobCollisionSystem
{
    private EntityQuery<ActorComponent> _actorQuery;

    public override void Initialize()
    {
        base.Initialize();
        _actorQuery = GetEntityQuery<ActorComponent>();
        SubscribeLocalEvent<MobCollisionComponent, MobCollisionMessage>(OnServerMobCollision);
    }

    private void OnServerMobCollision(Entity<MobCollisionComponent> ent, ref MobCollisionMessage args)
    {
        MoveMob(ent.Owner, args.Direction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MobCollisionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_actorQuery.HasComp(uid) || !PhysicsQuery.TryComp(uid, out var physics))
                continue;

            HandleCollisions((uid, comp, physics), frameTime);
        }
    }

    protected override void RaiseCollisionEvent(EntityUid uid, Vector2 direction)
    {
        RaiseLocalEvent(uid, new MobCollisionMessage()
        {
            Direction = direction,
        });
    }
}
