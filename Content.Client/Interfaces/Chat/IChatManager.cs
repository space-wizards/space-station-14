using System;
using Content.Client.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Interfaces.Chat
{
    public interface IChatManager
    {
        void Initialize();

        void FrameUpdate(FrameEventArgs delta);

        void SetChatBox(ChatBox chatBox);

        void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble);

        /// <summary>
        /// Current chat box control. This can be modified, so do not depend on saving a reference to this.
        /// </summary>
        ChatBox? CurrentChatBox { get; }

        /// <summary>
        /// Invoked when CurrentChatBox is resized (including after setting initial default size)
        /// </summary>
        event Action<ChatResizedEventArgs>? OnChatBoxResized;
    }
}
