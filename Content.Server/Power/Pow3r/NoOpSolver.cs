namespace Content.Server.Power.Pow3r
{
    public sealed class NoOpSolver : IPowerSolver
    {
        public void Tick(float frameTime, PowerState state, int parallel)
        {
            // Literally nothing.
        }
    }
}
