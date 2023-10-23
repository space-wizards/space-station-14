using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Timing;

using Content.Server.Physics.Components;
using Content.Shared.Follower.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using System.Numerics;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// A component which makes its entity periodically chaotic jumps arounds
/// </summary>
internal sealed class ChaoticJumpSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    public override void Initialize()
    {
        base.Initialize();
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
        Vector2 startPos = Transform(uid).WorldPosition;
        Vector2 targetPos = new Vector2();
        var direction = Angle.FromDegrees((double)(_random.Next(8) * 45)); //To Do: attach the selection of degrees not on world coordinates, but on grid coordinates.
        var range = _random.NextFloat(component.RangeMin, component.RangeMax);
        var ray = new CollisionRay(startPos, direction.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(Transform(uid).MapID, ray, range, uid, returnOnFirstHit: false).ToList();

        if (rayCastResults.Count > 0)
        {
            targetPos = Transform(rayCastResults[0].HitEntity).WorldPosition;
            targetPos = new Vector2(targetPos.X - (float) Math.Cos(direction), targetPos.Y - (float) Math.Sin(direction)); //offset so that the teleport does not take place directly inside the target
        }
        else
        {
            targetPos = new Vector2(startPos.X + range * (float)Math.Cos(direction), startPos.Y + range * (float) Math.Sin(direction));
        }

        Spawn(component.Effect, Transform(uid).Coordinates);

        _xform.SetWorldPosition(uid, targetPos);
        _physics.SetLinearVelocity(uid, new Vector2());
    }
}
