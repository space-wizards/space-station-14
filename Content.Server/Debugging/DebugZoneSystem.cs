using Content.Server.Atmos;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Content.Server.Debugging
{
    /// <summary>
    /// System which sends zone debug information to clients moving between zones.
    /// </summary>
    /// /// <remarks>
    /// Connects to the <see cref="EntitySystem"/> of the same name in <see cref="Content.Client"/>.
    /// </remarks>
    class DebugZoneSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IAtmosphereMap _atmosphereMap;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <summary>
        /// The last atmosphere to be sent to the client for debugging. Avoids duplicate messages.
        /// </summary>
        private ZoneAtmosphere _lastSentAtmosphere;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MoveEvent>(@event => OnMoved(@event));
        }

        [Conditional("DEBUG")]
        private void OnMoved(MoveEvent @event)
        {
            // Avoid sending as many zone messages as possible, because they are
            // beefy - don't send them for non-player moves and for moves which are
            // not between zones

            if (!@event.Sender.TryGetComponent<IActorComponent>(out var actor))
                return;

            var gridId = @event.Sender.Transform.GridID;



            var gridAtmosphere = _atmosphereMap.GetGridAtmosphereManager(gridId);

            var newPos = _mapManager.GetGrid(gridId).SnapGridCellFor(@event.NewPosition, SnapGridOffset.Center);
            var currentZone = gridAtmosphere.GetZone(newPos);

            if (currentZone == null || object.ReferenceEquals(currentZone, _lastSentAtmosphere))
                return;

            var zoneMessage = new ZoneInfo();
            zoneMessage.Cells = currentZone.TileIndices.ToArray();
            zoneMessage.Contents = null;

            RaiseNetworkEvent(zoneMessage, actor.playerSession.ConnectedClient);

            _lastSentAtmosphere = currentZone;
        }
    }
}
