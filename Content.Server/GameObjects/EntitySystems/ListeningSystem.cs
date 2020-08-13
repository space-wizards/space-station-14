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
        [Dependency] private readonly IMapManager _mapManager = default!;

        public void PingListeners(IEntity source, GridCoordinates sourcePos, string message)
        {
            foreach (var listener in ComponentManager.EntityQuery<ListeningComponent>())
            {
                var dist = sourcePos.Distance(_mapManager, listener.Owner.Transform.GridPosition);

                listener.PassSpeechData(message, source, dist);
            }
        }
    }
}
