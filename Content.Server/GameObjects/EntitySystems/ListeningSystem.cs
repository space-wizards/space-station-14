using Content.Server.GameObjects.Components;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    class ListeningSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

        public void PingListeners(IEntity source, GridCoordinates sourcePos, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<IListen>())
            {
                var listenerPos = listener.Owner.Transform.GridPosition;
                var dist = listenerPos.Distance(_mapManager, sourcePos);
                if (dist <= listener.GetListenRange())
                {
                    listener.HeardSpeech(message, source);
                }
            }
        }
    }
}
