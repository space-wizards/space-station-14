using Content.Client.Chat;
using Robust.Client;
using Robust.Shared.GameObjects;

namespace Content.Client.Interfaces.Chat
{
    public interface IChatManager
    {
        void Initialize();

        void FrameUpdate(RenderFrameEventArgs delta);

        void SetChatBox(ChatBox chatBox);

        void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble);
    }
}
