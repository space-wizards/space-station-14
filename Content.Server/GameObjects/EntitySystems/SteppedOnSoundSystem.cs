using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class SteppedOnSoundSystem : SharedSteppedOnSoundSystem
    {
        protected override Filter GetFilter(IEntity entity)
        {
            var player = entity.PlayerSession();
            var filter = Filter.Pvs(entity);

            return player != null ? filter.RemovePlayer(player) : filter;
        }
    }
}
