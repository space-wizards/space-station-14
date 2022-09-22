using Content.Server.Radio.Components;
using Content.Shared.Radio;
using JetBrains.Annotations;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public sealed class ListeningSystem : EntitySystem
    {
        public void PingListeners(EntityUid source, string message, RadioChannelPrototype? channel)
        {
            foreach (var listener in EntityManager.EntityQuery<IListen>(true))
            {
                // TODO: Listening code is hella stinky so please refactor it someone.
                // TODO: Map Position distance
                if (listener.CanListen(message, source, channel))
                {
                    listener.Listen(message, source, channel);
                }
            }
        }
    }
}
