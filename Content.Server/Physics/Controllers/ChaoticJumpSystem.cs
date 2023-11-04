using System.Linq;
using Content.Server.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using System.Numerics;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// A component which makes its entity periodically chaotic jumps arounds
/// </summary>
public sealed class ChaoticJumpSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChaoticJumpComponent>();
        while (query.MoveNext(out var uid, out var chaotic))
        {
            //Jump
            if (chaotic.NextJumpTime <= _gameTiming.CurTime)
            {
                Jump(uid, chaotic);
                chaotic.NextJumpTime = _gameTiming.CurTime + TimeSpan.FromSeconds(_random.NextFloat(chaotic.JumpMinInterval, chaotic.JumpMaxInterval));
            }
        }
    }

    private void Jump(EntityUid uid, ChaoticJumpComponent component)
    {
        var transform = Transform(uid);
        var startPos = transform.WorldPosition;
        var targetPos = new Vector2();
        var direction = _random.NextAngle();
        var range = _random.NextFloat(component.RangeMin, component.RangeMax);
        var ray = new CollisionRay(startPos, direction.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(transform.MapID, ray, range, uid, returnOnFirstHit: false).ToList();

        if (rayCastResults.Count > 0)
        {
            targetPos = rayCastResults[0].HitPos;
            targetPos = new Vector2(targetPos.X - (float) Math.Cos(direction), targetPos.Y - (float) Math.Sin(direction)); //offset so that the teleport does not take place directly inside the target
        }
        else
        {
            targetPos = new Vector2(startPos.X + range * (float)Math.Cos(direction), startPos.Y + range * (float) Math.Sin(direction));
        }

        Spawn(component.Effect, Transform(uid).Coordinates);

        _xform.SetWorldPosition(uid, targetPos);
    }
}
