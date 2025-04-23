using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        void Initialize();

        /// <summary>
        ///     Will refresh perms.
        /// </summary>
        event Action PermissionsUpdated;

        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions();
    }
}
