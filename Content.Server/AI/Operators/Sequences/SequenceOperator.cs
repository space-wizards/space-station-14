using System;
using System.Collections.Generic;

namespace Content.Server.AI.Operators.Sequences
{
    /// <summary>
    /// Sequential chain of operators
    /// Saves having to duplicate stuff like MoveTo and PickUp everywhere
    /// </summary>
    public abstract class SequenceOperator : AiOperator
    {
        public Queue<AiOperator> Sequence { get; protected set; }

        public override Outcome Execute(float frameTime)
        {
            if (Sequence.Count == 0)
            {
                return Outcome.Success;
            }

            var op = Sequence.Peek();
            op.Startup();
            var outcome = op.Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    op.Shutdown(outcome);
                    // Not over until all operators are done
                    Sequence.Dequeue();
                    return Outcome.Continuing;
                case Outcome.Continuing:
                    return Outcome.Continuing;
                case Outcome.Failed:
                    op.Shutdown(outcome);
                    Sequence.Clear();
                    return Outcome.Failed;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
