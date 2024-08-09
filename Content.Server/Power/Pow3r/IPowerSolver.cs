using Robust.Shared.Threading;

namespace Content.Server.Power.Pow3r
{
    public interface IPowerSolver
    {
        void Tick(float frameTime, PowerState state, IParallelManager parallel);
    }
}
