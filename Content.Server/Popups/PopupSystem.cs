using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Popups
{
    public sealed class PopupSystem : SharedPopupSystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void PopupCursor(string message, Filter filter, PopupType type=PopupType.Small)
        {
            // TODO REPLAYS
            // add variants that take in a EntityUid or ICommonSession
            // then remove any that send Filter.SinglePlayer() or single entity.
            // then default to recording replays
            // and manually remove any that shouldn't be replayed.
            RaiseNetworkEvent(new PopupCursorEvent(message, type), filter);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, Filter filter, bool replayRecord, PopupType type = PopupType.Small)
        {
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, coordinates), filter, replayRecord);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, PopupType type = PopupType.Small)
        {
            var mapPos = coordinates.ToMap(EntityManager);
            var filter = Filter.Empty().AddPlayersByPvs(mapPos, entManager: EntityManager, playerMan: _player, cfgMan: _cfg);
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, coordinates), filter);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, coordinates), recipient);
        }

        public override void PopupCoordinates(string message, EntityCoordinates coordinates, EntityUid recipient, PopupType type = PopupType.Small)
        {
            if (TryComp(recipient, out ActorComponent? actor))
                RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, coordinates), actor.PlayerSession);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter, PopupType type=PopupType.Small)
        {
            // TODO REPLAYS See above
            RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), filter);
        }
    }
}
