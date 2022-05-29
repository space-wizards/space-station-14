using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Stores what shuttle this entity is currently piloting.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class PilotComponent : Component
    {
        [ViewVariables] public SharedShuttleConsoleComponent? Console { get; set; }

        /// <summary>
        /// Where we started piloting from to check if we should break from moving too far.
        /// </summary>
        [ViewVariables] public EntityCoordinates? Position { get; set; }

        public const float BreakDistance = 0.25f;
    }
}
