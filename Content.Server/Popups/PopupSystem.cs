using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Popups
{
    public class PopupSystem : SharedPopupSystem
    {
        public override void PopupCursor(string message, Filter filter)
        {
            RaiseNetworkEvent(new PopupCursorEvent(message), filter);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter)
        {
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, coordinates), filter);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter)
        {
            RaiseNetworkEvent(new PopupEntityEvent(message, uid), filter);
        }

        public override Filter GetFilterFromEntity(IEntity entity)
        {
            return entity.TryGetComponent(out ActorComponent? actor)
                ? Filter.SinglePlayer(actor.PlayerSession) : Filter.Empty();
        }
    }
}
