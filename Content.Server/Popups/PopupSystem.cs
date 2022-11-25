using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        public override void PopupCursor(string message, Filter filter, PopupType type=PopupType.Small)
        {
            // TODO REPLAYS
            // add variants that take in a EntityUid or ICommonSession
            // then remove any that send Filter.SinglePlayer() or single entity.
            // then default to recording replays
            // and manually remove any that shouldn't be replayed.
            RaiseNetworkEvent(new PopupCursorEvent(message, type), filter);
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
