using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    class ListeningSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public void PingListeners(IEntity source, GridCoordinates sourcePos, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<IListen>())
            {
                // TODO: Map Position distance
                var listenerPos = listener.Owner.Transform.GridPosition;
                var dist = listenerPos.Distance(_mapManager, sourcePos);
                if (dist <= listener.ListenRange)
                {
                    listener.HeardSpeech(message, source);
                }
            }
        }
    }
}
