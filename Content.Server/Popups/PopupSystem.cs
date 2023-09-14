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

        public override void PopupEntity(string message, EntityUid uid, PopupType type = PopupType.Small)
        {
            var filter = Filter.Empty().AddPlayersByPvs(uid, entityManager:EntityManager, playerMan: _player, cfgMan: _cfg);
            RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), filter);
        }

        public override void PopupEntity(string message, EntityUid uid, EntityUid recipient, PopupType type=PopupType.Small)
        {
            if (TryComp(recipient, out ActorComponent? actor))
                RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), actor.PlayerSession);
        }

        public override void PopupClient(string message, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small)
        {
            // do nothing duh its for client only
        }


        public override void PopupEntity(string message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
        {
            RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), recipient);
        }

        public override void PopupEntity(string message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
        {
            RaiseNetworkEvent(new PopupEntityEvent(message, type, uid), filter, recordReplay);
        }
    }
}
