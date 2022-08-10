namespace Content.Server.AI.Operators.Generic
{
    public sealed class WaitOperator : AiOperator
    {
        private readonly float _waitTime;
        private float _accumulatedTime = 0.0f;

        public WaitOperator(float waitTime)
        {
            _waitTime = waitTime;
        }

        public override Outcome Execute(float frameTime)
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
