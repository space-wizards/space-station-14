using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        public override void PopupCursor(Filter filter, string message)
        {
            RaiseNetworkEvent(new PopupCursorEvent(message), filter);
        }

        public override void PopupCoordinates(Filter filter, string message, EntityCoordinates coordinates)
        {
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, coordinates), filter);
        }

        public override void PopupEntity(Filter filter, string message, EntityUid uid)
        {
            RaiseNetworkEvent(new PopupEntityEvent(message, uid), filter);
        }
    }
}
