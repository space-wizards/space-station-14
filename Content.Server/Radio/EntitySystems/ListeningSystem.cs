using Content.Server.Radio.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public class ListeningSystem : EntitySystem
    {
        public void PingListeners(IEntity source, string message)
        {
            foreach (var listener in EntityManager.EntityQuery<IListen>(true))
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
