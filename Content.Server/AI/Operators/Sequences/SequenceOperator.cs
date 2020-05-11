using System;
using System.Collections.Generic;

namespace Content.Server.AI.Operators.Sequences
{
    /// <summary>
    /// Sequential chain of operators
    /// Saves having to duplicate stuff like MoveTo and PickUp everywhere
    /// </summary>
    public abstract class SequenceOperator : IOperator
    {
        public Queue<IOperator> Sequence { get; protected set; }

        public Outcome Execute(float frameTime)
        {
            if (Sequence.Count == 0)
            {
                return Outcome.Success;
            }

            var outcome = Sequence.Peek().Execute(frameTime);

            switch (outcome)
            {
                case Outcome.Success:
                    // Not over until all operators are done
                    Sequence.Dequeue();
                    return Outcome.Continuing;
                case Outcome.Continuing:
                    return Outcome.Continuing;
                case Outcome.Failed:
                    Sequence.Clear();
                    return Outcome.Failed;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}