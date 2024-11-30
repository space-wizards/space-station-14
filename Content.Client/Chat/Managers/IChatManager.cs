using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        void Initialize(); // Collective mind edit
        event Action PermissionsUpdated; // Collective mind edit

        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions(); // Collective mind edit
    }
}
