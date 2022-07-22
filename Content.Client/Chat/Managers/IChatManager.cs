using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager
    {
        bool IsGhost { get; }

        void Initialize();

        public void SendMessage(ReadOnlyMemory<char> text, ChatSelectChannel channel);
    }
}
