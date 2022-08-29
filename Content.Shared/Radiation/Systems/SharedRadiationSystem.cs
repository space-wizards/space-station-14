using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Systems;

public abstract partial class SharedRadiationSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed class RadiationUpdate : EntityEventArgs
{
    public readonly Dictionary<EntityUid, Dictionary<Vector2i, float>> RadiationMap;
    public List<(Matrix3, Dictionary<Vector2i, float>)> SpaceMap;

    public RadiationUpdate(Dictionary<EntityUid, Dictionary<Vector2i, float>> radiationMap,
        List<(Matrix3, Dictionary<Vector2i, float>)> spaceMap)
    {
        RadiationMap = radiationMap;
        SpaceMap = spaceMap;
    }
}

[Serializable, NetSerializable]
public sealed class RadiationRaysUpdate : EntityEventArgs
{
    public readonly List<RadRayResult> Rays;

    public RadiationRaysUpdate(List<RadRayResult> rays)
    {
        Rays = rays;
    }
}

[Serializable, NetSerializable]
public sealed class RadRayResult
{
    public readonly EntityUid SourceUid;
    public readonly Vector2 SourcePos;
    public readonly EntityUid DestUid;
    public readonly Vector2 DestPos;
    public readonly MapId MapId;
    public readonly List<(Vector2, float)> Blockers;
    public readonly float SourceRads;
    public readonly float ReceivedRads;

    public RadRayResult(EntityUid sourceUid, Vector2 sourcePos,
        EntityUid destUid, Vector2 destPos, MapId mapId,
        List<(Vector2, float)> blockers, float sourceRads, float receivedRads)
    {
        SourceUid = sourceUid;
        SourcePos = sourcePos;
        DestUid = destUid;
        DestPos = destPos;
        MapId = mapId;
        Blockers = blockers;
        SourceRads = sourceRads;
        ReceivedRads = receivedRads;
    }

    public bool ReachedDestination => ReceivedRads > 0;

    public Vector2 LastPos
    {
        get
        {
            if (ReachedDestination)
                return DestPos;

            // this shouldn't really happen
            if (Blockers.Count == 0)
                return DestPos;

            var (lastBlocker, _) = Blockers.Last();
            return lastBlocker;
        }
    }
}

[Serializable, NetSerializable]
public sealed class RadiationGridcastUpdate : EntityEventArgs
{
    public List<RadiationRay> Rays;

    public RadiationGridcastUpdate(List<RadiationRay> rays)
    {
        Rays = rays;
    }
}

[Serializable, NetSerializable]
public sealed class RadiationRay
{
    public MapId MapId;
    public Vector2 Source;
    public Vector2 Destination;
    public float Rads;

    public EntityUid? Grid;
    public List<(Vector2i, float?)> VisitedTiles = new();

    public List<(Vector2, float)> Blockers = new();

    public bool IsGridcast => Grid != null;
    public bool ReachedDestination => Rads > 0;

    public Vector2 LastPos
    {
        get
        {
            if (ReachedDestination)
                return Destination;

            // this shouldn't really happen
            if (Blockers.Count == 0)
                return Destination;

            var (lastBlocker, _) = Blockers.Last();
            return lastBlocker;
        }
    }
}
