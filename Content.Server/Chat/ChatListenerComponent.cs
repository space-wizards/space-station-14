using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chat
{
    /// <summary>
    ///     Marks an entity as being able to receive chat messages
    ///     as an event. This is done to avoid raising events on every
    ///     single entity in radius.
    /// </summary>
    public class ChatListenerComponent : Component
    {
        public override string Name => "ChatListener";

        [DataField("listenRange")]
        public int ListenRange = 3;
    }
}
