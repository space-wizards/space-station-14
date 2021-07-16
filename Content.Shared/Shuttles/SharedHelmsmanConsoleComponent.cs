using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles
{
    /// <summary>
    /// Interact with to start piloting a shuttle.
    /// </summary>
    [NetworkedComponent()]
    public abstract class SharedHelmsmanConsoleComponent : Component
    {
        public override string Name => "HelmsmanConsole";
    }
}
