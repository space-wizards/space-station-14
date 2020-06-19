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
        public class UtilityAiDebugMessage : EntitySystemMessage
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
        public class RequestPathfindingGraphMessage : EntitySystemMessage {}

        [Serializable, NetSerializable]
        public class PathfindingGraphMessage : EntitySystemMessage
        {
            public Dictionary<int, List<Vector2>> Graph { get; }

            public PathfindingGraphMessage(Dictionary<int, List<Vector2>> graph)
            {
                Graph = graph;
            }
        }

        public class AStarRouteDebug
        {
            public EntityUid EntityUid { get; }
            public Queue<TileRef> Route { get; }
            public Dictionary<TileRef, TileRef> CameFrom { get; }
            public Dictionary<TileRef, float> GScores { get; }
            public HashSet<TileRef> ClosedTiles { get; }
            public double TimeTaken { get; }

            public AStarRouteDebug(
                EntityUid uid,
                Queue<TileRef> route,
                Dictionary<TileRef, TileRef> cameFrom,
                Dictionary<TileRef, float> gScores,
                HashSet<TileRef> closedTiles,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                CameFrom = cameFrom;
                GScores = gScores;
                ClosedTiles = closedTiles;
                TimeTaken = timeTaken;
            }
        }

        public class JpsRouteDebug
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
        public class AStarRouteMessage : EntitySystemMessage
        {
            public readonly EntityUid EntityUid;
            public readonly IEnumerable<Vector2> Route;
            public readonly Dictionary<Vector2, Vector2> CameFrom;
            public readonly Dictionary<Vector2, float> GScores;
            public readonly List<Vector2> ClosedTiles;
            public double TimeTaken;

            public AStarRouteMessage(
                EntityUid uid,
                IEnumerable<Vector2> route,
                Dictionary<Vector2, Vector2> cameFrom,
                Dictionary<Vector2, float> gScores,
                List<Vector2> closedTiles,
                double timeTaken)
            {
                EntityUid = uid;
                Route = route;
                CameFrom = cameFrom;
                GScores = gScores;
                ClosedTiles = closedTiles;
                TimeTaken = timeTaken;
            }
        }

        [Serializable, NetSerializable]
        public class JpsRouteMessage : EntitySystemMessage
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
    }
}
