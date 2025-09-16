using Content.Server.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Movement.Systems;

public sealed class LagCompensationSystem : SharedLagCompensationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    // I figured 500 ping is max
    // Max ping I've had is 350ms from aus to spain.
    public static readonly TimeSpan BufferTime = TimeSpan.FromMilliseconds(500);

    public override void Initialize()
    {
        base.Initialize();
        Log.Level = LogLevel.Info;
        SubscribeLocalEvent<LagCompensationComponent, MoveEvent>(OnLagMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var earliestTime = curTime - BufferTime;

        // Cull any old ones from active updates
        // Probably fine to include ignored.
        var query = AllEntityQuery<LagCompensationComponent>();

        while (query.MoveNext(out var comp))
        {
            while (comp.Positions.TryPeek(out var pos))
            {
                if (pos.Time >= earliestTime)
                    break;

                comp.Positions.Dequeue();
            }
        }
    }

    private void OnLagMove(EntityUid uid, LagCompensationComponent component, ref MoveEvent args)
    {
        if (!args.NewPosition.EntityId.IsValid())
            return; // probably being sent to nullspace for deletion.

        component.Positions.Enqueue((_timing.CurTime, _timing.CurTick, args.NewPosition, args.NewRotation));
    }

    public override (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(
        Entity<TransformComponent?> ent,
        GameTick tick)
    {
        if (!Resolve(ent, ref ent.Comp))
            return (EntityCoordinates.Invalid, Angle.Zero);

        if (!TryComp<LagCompensationComponent>(ent, out var lag) || lag.Positions.Count == 0)
            return (ent.Comp.Coordinates, ent.Comp.LocalRotation);

        var angle = Angle.Zero;
        var coordinates = EntityCoordinates.Invalid;

        // Replay the position history, starting with the oldest known position, up until we find an entry that was
        // inserted after the requested tick.
        foreach (var pos in lag.Positions)
        {
            if (pos.Tick > tick)
                break;

            coordinates = pos.Coords;
            angle = pos.Angle;
        }

        if (coordinates != default)
            return (coordinates, angle);

        var oldest = lag.Positions.Peek();
        return (oldest.Coords, oldest.Angle);
    }
}
