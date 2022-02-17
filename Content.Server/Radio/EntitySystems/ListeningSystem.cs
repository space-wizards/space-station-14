using Content.Server.Radio.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public sealed class ListeningSystem : EntitySystem
    {
        public void PingListeners(EntityUid source, string message)
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
