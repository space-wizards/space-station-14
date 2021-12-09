using System.Collections.Generic;
using Content.Server.AI.Operators.Inventory;
using Content.Server.AI.Operators.Movement;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Operators.Sequences
{
    public class GoPickupEntitySequence : SequenceOperator
    {
        public GoPickupEntitySequence(EntityUid owner, EntityUid target)
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
