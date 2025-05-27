using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
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
        if (!CfgManager.GetCVar(CCVars.MovementMobPushing))
            return;

        if (_timing.IsFirstTimePredicted)
        {
            var player = _player.LocalEntity;

            if (MobQuery.TryComp(player, out var comp) && PhysicsQuery.TryComp(player, out var physics))
            {
                HandleCollisions((player.Value, comp, physics), frameTime);
            }
        }

        base.Update(frameTime);
    }

    protected override void RaiseCollisionEvent(EntityUid uid, Vector2 direction, float speedMod)
    {
        RaisePredictiveEvent(new MobCollisionMessage()
        {
            Direction = direction,
            SpeedModifier = speedMod,
        });
    }
}
