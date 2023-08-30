using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager
    {
        void Initialize();

        public void SendMessage(string text, ChatSelectChannel channel);
    }
}
