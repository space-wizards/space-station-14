using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    /// Ignores gravity entirely.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class MovementIgnoreGravityComponent : Component
    {
        /// <summary>
        /// Whether gravity is on or off for this object. This will always override the current Gravity State.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Weightless;
    }
}
