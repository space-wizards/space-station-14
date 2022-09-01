using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems
{
    public sealed class PrayerSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        public void SendSubtleMessage(IPlayerSession target, string messageString, string popupMessage = "You hear a voice in your head...")
        {
            if (target.AttachedEntity == null)
                return;
            // This works, but if someone can find a solution to
            // sending a message to a specific client's chat box I'd really
            // appreciate it.
            _popupSystem.PopupEntity(popupMessage + " " + messageString, target.AttachedEntity.Value, Filter.Empty().AddPlayer(target), PopupType.Large);
        }
    }
}
