using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ShuttleConsoleComponent : SharedShuttleConsoleComponent
    {
        /// <summary>
        /// Set by shuttlesystem if the grid should no longer be pilotable.
        /// </summary>
        [ViewVariables]
        public bool CanPilot = true;

        [ViewVariables]
        public readonly List<PilotComponent> SubscribedPilots = new();

        /// <summary>
        /// How much should the pilot's eye be zoomed by when piloting using this console?
        /// </summary>
        [DataField("zoom")]
        public Vector2 Zoom = new(1.5f, 1.5f);
    }
}
