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
            BehaviorSets.Add(typeof(ClothingBehaviorSet), new ClothingBehaviorSet(SelfEntity));
            BehaviorSets.Add(typeof(HungerBehaviorSet), new HungerBehaviorSet(SelfEntity));
            BehaviorSets.Add(typeof(ThirstBehaviorSet), new ThirstBehaviorSet(SelfEntity));;
            BehaviorSets.Add(typeof(SpirateBehaviorSet), new SpirateBehaviorSet(SelfEntity));
            BehaviorSets.Add(typeof(IdleBehaviorSet), new IdleBehaviorSet(SelfEntity));
            SortActions();
        }
    }
}
