using Content.Server.AI.Utility.Actions;
using Content.Server.AI.Utility.Actions.Test;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility.BehaviorSets
{
    public sealed class PathingDummyBehaviorSet : BehaviorSet
    {
        public PathingDummyBehaviorSet(IEntity owner) : base(owner)
        {
            Actions = new IAiUtility[]
            {
                new MoveRightAndLeftTen(owner),
            };
        }
    }
}
