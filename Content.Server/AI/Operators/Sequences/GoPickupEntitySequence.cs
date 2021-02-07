using System.Collections.Generic;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.Operators.Sequences
{
    public class GoPickupEntitySequence : SequenceOperator
    {
        public GoPickupEntitySequence(IEntity owner, IEntity target)
        {
            Sequence = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(owner, target, requiresInRangeUnobstructed: true),
                new OpenStorageOperator(owner, target),
                new PickupEntityOperator(owner, target),
            });
        }
    }
}