using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        public override void PopupCursor(string message, PopupType type = PopupType.Small)
        {
            // No local user.
        }

        public override void PopupCursor(string message, ICommonSession recipient, PopupType type=PopupType.Small)
        {
            RaiseNetworkEvent(new PopupCursorEvent(message, type), recipient);
        }

        public override void PopupCursor(string message, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (TryComp(recipient, out ActorComponent? actor))
                RaiseNetworkEvent(new PopupCursorEvent(message, type), actor.PlayerSession);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter, PopupType type=PopupType.Small)
        {
            // TODO REPLAYS See above
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, coordinates), filter);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter, PopupType type=PopupType.Small)
        {
            // TODO REPLAYS See above
            RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), filter);
        }
    }
}
