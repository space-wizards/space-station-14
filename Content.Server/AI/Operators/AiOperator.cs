
namespace Content.Server.AI.Operators
{
    public abstract class AiOperator
    {
        public bool HasStartup { get; private set; }

        public bool HasShutdown { get; private set; }

        /// <summary>
        /// Called once when the AiLogicProcessor starts this action
        /// </summary>
        /// <returns>true if it hasn't started up previously</returns>
        public virtual bool Startup()
        {
            // If we've already startup then no point continuing
            // This signals to the override that it's already startup
            // Should probably throw but it made some code elsewhere marginally easier
            if (HasStartup)
                return false;

            HasStartup = true;
            return true;
        }

        /// <summary>
        /// Called once when the AiLogicProcessor is done with this action if the outcome is successful or fails.
        /// </summary>
        public virtual bool Shutdown(Outcome outcome)
        {
            if (HasShutdown)
                return false;

            HasShutdown = true;
            return true;
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
