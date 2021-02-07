using Content.Server.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ListeningSystem : EntitySystem
    {
        public void PingListeners(IEntity source, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<IListen>(true))
            {
                // TODO: Map Position distance
                if (listener.CanListen(message, source))
                {
                    listener.Listen(message, source);
                }
            }
        }
    }
}
