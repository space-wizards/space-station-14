using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private const float MinRads = 0.1f;

    public void RaycastUpdate()
    {
        var list = new List<RadRayResult>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var blockerQuery = GetEntityQuery<RadiationBlockerComponent>();
        foreach (var source in EntityQuery<RadiationSourceComponent>())
        {
            var sourceUid = source.Owner;
            var sourcePos = Transform(sourceUid).MapPosition;

            foreach (var dest in EntityQuery<RadiationReceiverComponent>())
            {
                var ray = Irradiate(sourceUid, source, sourcePos, dest, blockerQuery);
                if (ray != null)
                    list.Add(ray);
            }
        }

        Logger.Info($"Raycasted radiation {stopwatch.Elapsed.TotalMilliseconds}ms");

        RaiseNetworkEvent(new RadiationRaysUpdate(list));
    }

    private RadRayResult? Irradiate(EntityUid sourceUid, RadiationSourceComponent source, MapCoordinates sourcePos,
        RadiationReceiverComponent dest, EntityQuery<RadiationBlockerComponent> blockerQuery)
    {
        var destUid = dest.Owner;
        var destPos = Transform(destUid).MapPosition;

        if (sourcePos.MapId != destPos.MapId)
            return null;

        var dir = destPos.Position - sourcePos.Position;
        var dist = dir.Length;

        // inverse square law
        var rads = source.RadsPerSecond / dist;
        if (rads <= MinRads)
            return null;

        // do raycast to the physics
        var ray = new CollisionRay(sourcePos.Position, dir.Normalized, (int) CollisionGroup.Impassable);
        var results = _physicsSystem.IntersectRay(sourcePos.MapId, ray,
            dist, returnOnFirstHit: false);

        var blockers = new List<(Vector2, float)>();
        foreach (var obstacle in results)
        {
            if (!blockerQuery.TryGetComponent(obstacle.HitEntity, out var blocker))
                continue;

            rads -= blocker.RadResistance;
            blockers.Add((obstacle.HitPos, rads));
            if (rads <= MinRads)
            {
                return new RadRayResult(sourceUid, sourcePos.Position, destUid, destPos.Position,
                    sourcePos.MapId, blockers, source.RadsPerSecond, 0f);
            }
        }

        return new RadRayResult(sourceUid, sourcePos.Position, destUid, destPos.Position,
            sourcePos.MapId, blockers, source.RadsPerSecond, rads);
    }
}
