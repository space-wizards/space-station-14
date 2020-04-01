using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.AI
{
    public abstract class SharedAiDebugComponent : Component
    {
        public override string Name => "AIDebugger";
        public override uint? NetID => ContentNetIDs.AI_DEBUG;
    }

    [Serializable, NetSerializable]
    public class UtilityAiDebugMessage : ComponentMessage
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
}
