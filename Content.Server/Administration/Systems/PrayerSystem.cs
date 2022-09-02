using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Administration.Systems
{
    public sealed class PrayerSystem : SharedPrayerSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        /// <summary>
        /// Sends a popup and a chat message to the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="messageString"></param>
        /// <param name="popupMessage"></param>
        public void SendSubtleMessage(IPlayerSession target, string messageString, string popupMessage = "You hear a voice in your head...")
        {
            if (target.AttachedEntity == null)
                return;
            _popupSystem.PopupEntity(popupMessage, target.AttachedEntity.Value, Filter.Empty().AddPlayer(target), PopupType.Large);
            _chatManager.ChatMessageToOne(ChatChannel.Local, messageString, popupMessage + " \"{0}\"", EntityUid.Invalid, false, target.ConnectedClient);
        }

        public void Pray(ICommonSession player, string prayed)
        {
            var msg = new PrayTextMessage(prayed);
            RaiseNetworkEvent(msg, Filter.Empty().AddPlayer(player));
        }
    }
}
