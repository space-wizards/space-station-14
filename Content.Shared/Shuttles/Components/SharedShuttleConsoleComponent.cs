using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Interact with to start piloting a shuttle.
    /// </summary>
    [NetworkedComponent()]
    public abstract class SharedShuttleConsoleComponent : Component
    {
        public override string Name => "ShuttleConsole";
    }
}
