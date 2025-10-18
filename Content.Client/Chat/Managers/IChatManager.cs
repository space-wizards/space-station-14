using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        public void SendMessage(string text, ChatSelectChannel channel);
    }
}
