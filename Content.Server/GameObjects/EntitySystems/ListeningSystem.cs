using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class ListeningSystem : EntitySystem
    {
        public void PingListeners(IEntity source, EntityCoordinates sourcePos, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<ListeningComponent>())
            {
                if (!sourcePos.TryDistance(EntityManager, listener.Owner.Transform.Coordinates, out var distance))
                {
                    return;
                }

                listener.PassSpeechData(message, source, distance);
            }
        }
    }
}
