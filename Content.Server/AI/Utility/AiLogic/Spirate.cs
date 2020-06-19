using Content.Server.AI.Utility.BehaviorSets;
using JetBrains.Annotations;
using Robust.Server.AI;

namespace Content.Server.AI.Utility.AiLogic
{
    [AiLogicProcessor("Spirate")]
    [UsedImplicitly]
    public sealed class Spirate : UtilityAi
    {
        public override void Setup()
        {
            base.Setup();
            AddBehaviorSet(new ClothingBehaviorSet(SelfEntity), false);
            AddBehaviorSet(new HungerBehaviorSet(SelfEntity), false);
            AddBehaviorSet(new ThirstBehaviorSet(SelfEntity), false);
            AddBehaviorSet(new IdleBehaviorSet(SelfEntity), false);
            AddBehaviorSet(new SpirateBehaviorSet(SelfEntity), false);
            SortActions();
        }
    }
}
