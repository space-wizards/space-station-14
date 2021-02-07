using Content.Server.AI.Utility.BehaviorSets;
using JetBrains.Annotations;
using Robust.Server.AI;

namespace Content.Server.AI.Utility.AiLogic
{
    [AiLogicProcessor("Xeno")]
    [UsedImplicitly]
    public sealed class Xeno : UtilityAi
    {
        public override void Setup()
        {
            base.Setup();
            AddBehaviorSet(new IdleBehaviorSet(SelfEntity), false);
            AddBehaviorSet(new UnarmedAttackPlayersBehaviorSet(SelfEntity), false);
            SortActions();
        }
    }
}