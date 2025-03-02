using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        public void SendMessage(string text, ChatSelectChannel channel, RadioChannelPrototype? radioChannel = null);

        public void SendMessage(string text, string channel);

        public void SendMessage(string text, CommunicationChannelPrototype channel);
    }
}
