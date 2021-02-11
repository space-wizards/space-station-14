using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Idle;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public class IdleBehaviorSet : BehaviorSet
    {
        public IdleBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new CloseLastEntityStorage(Owner), 
                new WanderAndWait(Owner),
            };
        }
    }
}
