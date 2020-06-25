using Content.Server.AI.Utility.BehaviorSets;
using JetBrains.Annotations;
using Robust.Server.AI;

namespace Content.Server.AI.Utility.AiLogic
{
    [AiLogicProcessor("PathingDummy")]
    [UsedImplicitly]
    public sealed class PathingDummy : UtilityAi
    {
        public override void Setup()
        {
            base.Setup();
            BehaviorSets.Add(typeof(PathingDummyBehaviorSet), new PathingDummyBehaviorSet(SelfEntity));
            SortActions();
        }
    }
}
