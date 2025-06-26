using Content.Server.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using System.Numerics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// A component which makes its entity periodically chaotic jumps arounds
/// </summary>
public sealed class ChaoticJumpSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaoticJumpComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ChaoticJumpComponent> chaotic, ref MapInitEvent args)
    {
        //So the entity doesn't teleport instantly. For tesla, for example, it's important for it to eat tesla's generator.
        chaotic.Comp.NextJumpTime = _gameTiming.CurTime + TimeSpan.FromSeconds(_random.NextFloat(chaotic.Comp.JumpMinInterval, chaotic.Comp.JumpMaxInterval));
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ChaoticJumpComponent>();
        while (query.MoveNext(out var uid, out var chaotic))
        {
            //Jump
            if (chaotic.NextJumpTime <= _gameTiming.CurTime)
            {
                Jump(uid, chaotic);
                chaotic.NextJumpTime += TimeSpan.FromSeconds(_random.NextFloat(chaotic.JumpMinInterval, chaotic.JumpMaxInterval));
            }
        }
    }

    private void Jump(EntityUid uid, ChaoticJumpComponent component)
    {
        var transform = Transform(uid);

        var startPos = _transform.GetWorldPosition(uid);
        Vector2 targetPos;

        var direction = _random.NextAngle();
        var range = _random.NextFloat(component.RangeMin, component.RangeMax);
        var ray = new CollisionRay(startPos, direction.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(transform.MapID, ray, range, uid, returnOnFirstHit: false).FirstOrNull();

        if (rayCastResults != null)
        {
            targetPos = rayCastResults.Value.HitPos;
            targetPos = new Vector2(targetPos.X - (float) Math.Cos(direction), targetPos.Y - (float) Math.Sin(direction)); //offset so that the teleport does not take place directly inside the target
        }
        else
        {
            targetPos = new Vector2(startPos.X + range * (float) Math.Cos(direction), startPos.Y + range * (float) Math.Sin(direction));
        }

        Spawn(component.Effect, transform.Coordinates);

        _transform.SetWorldPosition(uid, targetPos);
    }
}
