using Content.Server.AI.Utility.BehaviorSets;
using JetBrains.Annotations;
using Robust.Server.AI;

namespace Content.Server.AI.Utility.AiLogic
{
    [AiLogicProcessor("Mimic")]
    [UsedImplicitly]
    public sealed class Mimic : UtilityAi
    {
        public override void Setup()
        {
            base.Setup();
            AddBehaviorSet(new UnarmedAttackPlayersBehaviorSet(SelfEntity), false);
            SortActions();
        }
    }
}