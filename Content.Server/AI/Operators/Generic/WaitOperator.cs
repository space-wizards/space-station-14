using Content.Server.AI.HTN.Tasks.Primitive.Operators;

namespace Content.Server.AI.Operators.Generic
{
    public class WaitOperator : IOperator
    {
        private readonly float _waitTime;
        private float _accumulatedTime = 0.0f;

        public WaitOperator(float waitTime)
        {
            _waitTime = waitTime;
        }

        public Outcome Execute(float frameTime)
        {
            if (_accumulatedTime < _waitTime)
            {
                _accumulatedTime += frameTime;
                return Outcome.Continuing;
            }

            return Outcome.Success;
        }
    }
}
