using Content.Server.Radio.Components;
using Content.Shared.Radio;
using JetBrains.Annotations;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public sealed class ListeningSystem : EntitySystem
    {
        public void PingListeners(EntityUid source, string message, RadioChannelPrototype channel)
        {
            foreach (var listener in EntityManager.EntityQuery<IListen>(true))
            {
                // TODO: Map Position distance
                if (listener.CanListen(message, source))
                {
                    listener.Listen(message, source, channel);
                }
            }
        }
    }
}
