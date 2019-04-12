using Content.Client.Chat;

namespace Content.Client.Interfaces.Chat
{
    public interface IChatManager
    {
        void Initialize();

        void SetChatBox(ChatBox chatBox);
    }
}
