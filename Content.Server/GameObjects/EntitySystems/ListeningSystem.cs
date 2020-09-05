using Content.Server.Interfaces;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    class ListeningSystem : EntitySystem
    {
        public void PingListeners(IEntity source, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<IListen>())
            {
                // TODO: Map Position distance
                if (listener.CanHear(message, source))
                {
                    listener.Broadcast(message, source);
                }
            }
        }
    }
}
