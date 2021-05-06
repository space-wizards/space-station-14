using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class SlipperySystem : SharedSlipperySystem
    {
        protected override Filter GetFilter(IEntity entity)
        {
            var player = entity.PlayerSession();

            return player == null ? Filter.Pvs(entity) : Filter.Pvs(entity).RemovePlayer(player);
        }
    }
}
