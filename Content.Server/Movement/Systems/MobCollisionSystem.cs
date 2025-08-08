using System.Numerics;
using Content.Shared.CCVar;
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
        MoveMob((ent.Owner, ent.Comp, Transform(ent.Owner)), args.Direction, args.SpeedModifier);
    }

    public override void Update(float frameTime)
    {
        if (!CfgManager.GetCVar(CCVars.MovementMobPushing))
            return;

        var query = EntityQueryEnumerator<MobCollisionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_actorQuery.HasComp(uid) || !PhysicsQuery.TryComp(uid, out var physics))
                continue;

            HandleCollisions((uid, comp, physics), frameTime);
        }

        base.Update(frameTime);
    }

    protected override void RaiseCollisionEvent(EntityUid uid, Vector2 direction, float speedMod)
    {
        RaiseLocalEvent(uid, new MobCollisionMessage()
        {
            Direction = direction,
            SpeedModifier = speedMod,
        });
    }
}
