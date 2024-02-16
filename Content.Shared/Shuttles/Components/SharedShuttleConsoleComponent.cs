using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Interact with to start piloting a shuttle.
    /// </summary>
    [NetworkedComponent]
    public abstract partial class SharedShuttleConsoleComponent : Component
    {

    }

    [Serializable, NetSerializable]
    public enum ShuttleConsoleUiKey : byte
    {
        Key,
    }
}
