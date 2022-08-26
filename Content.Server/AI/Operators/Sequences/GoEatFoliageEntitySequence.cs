using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.Operators.Sequences;
using Content.Server.Nyanotrasen.AI.Operators.Nutrition;

namespace Content.Server.Nyanotrasen.AI.Operators.Sequences
{
    public sealed class GoEatFoliageEntitySequence : SequenceOperator
    {
        public GoEatFoliageEntitySequence(EntityUid owner, EntityUid target)
        {
            Sequence = new Queue<AiOperator>(new AiOperator[]
            {
                new MoveToEntityOperator(owner, target, requiresInRangeUnobstructed: true),
                new EatFoliageOperator(owner, target),
            });
        }
    }
}
