using Robust.Shared.GameObjects;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Sends a trigger when signal is received.
    /// </summary>
    [RegisterComponent]
    public sealed class TriggerOnSignalComponent : Component
    {
        public const string Port = "Trigger";
    }
}