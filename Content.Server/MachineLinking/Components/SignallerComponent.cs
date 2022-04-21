using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Components
{
    /// <summary>
    /// Sends out a signal to machine linked objects.
    /// </summary>
    [RegisterComponent]
    public sealed class SignallerComponent : Component
    {
        public const string Port = "Pressed";
    }
}
