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
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public void PingListeners(IEntity source, GridCoordinates sourcePos, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<IListen>())
            {
                var listenerPos = listener.GetListenerPosition();
                var dist = listenerPos.Distance(_mapManager, sourcePos);
                if (dist <= listener.GetListenRange())
                {
                    listener.HeardSpeech(message, source);
                }
            }
        }
    }
}
