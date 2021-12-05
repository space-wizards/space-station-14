using Robust.Shared.GameObjects;

namespace Content.Server.Radio.Components
{
    /// <summary>
    ///     Interface for objects such as radios meant to have an effect when speech is
    ///     heard. Requires component reference.
    /// </summary>
    public interface IListen : IComponent
    {
        int ListenRange { get; }

        bool CanListen(string message, EntityUid source);

        void Listen(string message, EntityUid speaker);
    }
}
