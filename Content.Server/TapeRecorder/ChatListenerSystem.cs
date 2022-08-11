using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server.TapeRecorder
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class ChatListenerSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
        }
    }

    public sealed class ChatMessageHeardNearbyEvent : EntityEventArgs
    {
        /// <summary>
        ///     message
        /// </summary>
        public ChatChannel Channel;

        public string Message;

        public string MessageWrap;

        public EntityUid Source;

        public bool HideChat;

        public ChatMessageHeardNearbyEvent(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat)
        {
            Channel = channel;
            Message = message;
            MessageWrap = messageWrap;
            Source = source;
            HideChat = hideChat;
        }
    }
}
