using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        // CHAT-TODO: We probably wanna kill RadioChannelPrototype, or at least make sure it's more cleanly integrated.
        // Haven't done so yet though because we wanna make it easy for forks to move over to the new system.
        public void SendMessage(string text, ChatSelectChannel channel, RadioChannelPrototype? radioChannel = null);

        public void SendMessage(string text, string channel);

        public void SendMessage(string text, CommunicationChannelPrototype channel);
    }
}
