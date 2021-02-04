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

        void ToggleDeadChatButtonVisibility(bool visibility);
    }
}
