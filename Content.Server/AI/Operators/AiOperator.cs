
using System;

namespace Content.Server.AI.Operators
{
    public abstract class AiOperator
    {
        public bool HasStartup => _hasStartup;
        private bool _hasStartup = false;
        private bool _hasShutdown = false;

        /// <summary>
        /// Called once when the AiLogicProcessor starts this action
        /// </summary>
        public virtual bool TryStartup()
        {
            // If we've already startup then no point continuing
            // This signals to the override that it's already startup
            // Should probably throw but it made some code elsewhere marginally easier
            if (_hasStartup)
            {
                return false;
            }
            
            _hasStartup = true;
            return true;
        }

        /// <summary>
        /// Called once when the AiLogicProcessor is done with this action if the outcome is successful or fails.
        /// </summary>
        public virtual void Shutdown(Outcome outcome)
        {
            if (_hasShutdown)
            {
                throw new InvalidOperationException("AiOperator has already shutdown");
            }

            _hasShutdown = true;
        }

        /// <summary>
        /// Called every tick for the AI
        /// </summary>
        /// <param name="frameTime"></param>
        /// <returns></returns>
        public abstract Outcome Execute(float frameTime);
    }

    public enum Outcome
    {
        Success,
        Continuing,
        Failed,
    }
}