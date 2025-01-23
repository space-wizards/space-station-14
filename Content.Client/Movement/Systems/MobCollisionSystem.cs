using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class MobCollisionSystem : SharedMobCollisionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalEntity;

        if (!MobQuery.TryComp(player, out var comp) || !TryComp(player, out PhysicsComponent? physics))
            return;

        HandleCollisions((player.Value, comp, physics), frameTime);
    }

    protected override void RaiseCollisionEvent(EntityUid uid, Vector2 direction)
    {
        RaisePredictiveEvent(new MobCollisionMessage()
        {
            Direction = direction,
        });
    }
}
