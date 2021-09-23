using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.Chat
{
    /// <summary>
    ///     Handles sending chat events to
    /// </summary>
    public class ListenerSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;

        /// <summary>
        ///     Sends a ChatMessageListenedEvent to all listeners in radius around a source.
        /// </summary>
        public void SendToListeners(IEntity source, string message, int range)
        {
            Box2 box = Box2.CenteredAround(source.Transform.WorldPosition, new Vector2(range * 2, range * 2));
            var ev = new ChatMessageListenedEvent(source.Uid, message);

            _lookup.FastEntitiesIntersecting(source.Transform.MapID, ref box, entity =>
            {
                if (!entity.HasComponent<ChatListenerComponent>()) return;
                RaiseLocalEvent(entity.Uid, ev, false);
            }, LookupFlags.IncludeAnchored);
        }
    }

    public class ChatMessageListenedEvent : EntityEventArgs
    {
        public EntityUid Speaker;
        public string Message;

        public ChatMessageListenedEvent(EntityUid speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }
    }
}
