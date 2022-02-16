using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.AI
{
    public static class SharedAiDebug
    {
        #region Mob Debug
        [Serializable, NetSerializable]
        public sealed class UtilityAiDebugMessage : EntityEventArgs
        {
            public EntityUid EntityUid { get; }
            public double PlanningTime { get; }
            public float ActionScore { get; }
            public string FoundTask { get; }
            public int ConsideredTaskCount { get; }

            public UtilityAiDebugMessage(
                EntityUid entityUid,
                double planningTime,
                float actionScore,
                string foundTask,
                int consideredTaskCount)
            {
                EntityUid = entityUid;
                PlanningTime = planningTime;
                ActionScore = actionScore;
                FoundTask = foundTask;
                ConsideredTaskCount = consideredTaskCount;
            }
        }
        #endregion
        #region Pathfinder Debug
        /// <summary>
        /// Client asks the server for the pathfinding graph details
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class RequestPathfindingGraphMessage : EntityEventArgs {}

        [Serializable, NetSerializable]
        public sealed class PathfindingGraphMessage : EntityEventArgs
        {
            public Dictionary<int, List<Vector2>> Graph { get; }

            public PathfindingGraphMessage(Dictionary<int, List<Vector2>> graph)
            {
                Graph = graph;
            }
        }

        public sealed class AStarRouteDebug
        {
            public EntityUid EntityUid { get; }
            public Queue<TileRef> Route { get; }
            public Dictionary<TileRef, TileRef> CameFrom { get; }
            public Dictionary<TileRef, float> GScores { get; }
            public double TimeTaken { get; }

            public AStarRouteDebug(
                EntityUid uid,
                Queue<TileRef> route,
                Dictionary<TileRef, TileRef> cameFrom,
                Dictionary<TileRef, float> gScores,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                CameFrom = cameFrom;
                GScores = gScores;
                TimeTaken = timeTaken;
            }
        }

        public sealed class JpsRouteDebug
        {
            public EntityUid EntityUid { get; }
            public Queue<TileRef> Route { get; }
            public HashSet<TileRef> JumpNodes { get; }
            public double TimeTaken { get; }

            public JpsRouteDebug(
                EntityUid uid,
                Queue<TileRef> route,
                HashSet<TileRef> jumpNodes,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                JumpNodes = jumpNodes;
                TimeTaken = timeTaken;
            }
        }

        [Serializable, NetSerializable]
        public sealed class AStarRouteMessage : EntityEventArgs
        {
            public readonly EntityUid EntityUid;
            public readonly IEnumerable<Vector2> Route;
            public readonly Dictionary<Vector2, Vector2> CameFrom;
            public readonly Dictionary<Vector2, float> GScores;
            public double TimeTaken;

            public AStarRouteMessage(
                EntityUid uid,
                IEnumerable<Vector2> route,
                Dictionary<Vector2, Vector2> cameFrom,
                Dictionary<Vector2, float> gScores,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                CameFrom = cameFrom;
                GScores = gScores;
                TimeTaken = timeTaken;
            }
        }

        [Serializable, NetSerializable]
        public sealed class JpsRouteMessage : EntityEventArgs
        {
            public readonly EntityUid EntityUid;
            public readonly IEnumerable<Vector2> Route;
            public readonly List<Vector2> JumpNodes;
            public double TimeTaken;

            public JpsRouteMessage(
                EntityUid uid,
                IEnumerable<Vector2> route,
                List<Vector2> jumpNodes,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                JumpNodes = jumpNodes;
                TimeTaken = timeTaken;
            }
        }
        #endregion
        #region Reachable Debug
        [Serializable, NetSerializable]
        public sealed class ReachableChunkRegionsDebugMessage : EntityEventArgs
        {
            public GridId GridId { get; }
            public Dictionary<int, Dictionary<int, List<Vector2>>> Regions { get; }

            public ReachableChunkRegionsDebugMessage(GridId gridId, Dictionary<int, Dictionary<int, List<Vector2>>> regions)
            {
                GridId = gridId;
                Regions = regions;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ReachableCacheDebugMessage : EntityEventArgs
        {
            public GridId GridId { get; }
            public Dictionary<int, List<Vector2>> Regions { get; }
            public bool Cached { get; }

            public ReachableCacheDebugMessage(GridId gridId, Dictionary<int, List<Vector2>> regions, bool cached)
            {
                GridId = gridId;
                Regions = regions;
                Cached = cached;
            }
        }

        /// <summary>
        ///     Send if someone is subscribing to reachable regions for NPCs.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class SubscribeReachableMessage : EntityEventArgs {}

        /// <summary>
        ///     Send if someone is unsubscribing to reachable regions for NPCs.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class UnsubscribeReachableMessage : EntityEventArgs {}
        #endregion
    }
}
