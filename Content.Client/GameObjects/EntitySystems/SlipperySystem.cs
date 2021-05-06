using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.GameObjects.EntitySystems
{
    internal sealed class SlipperySystem : SharedSlipperySystem
    {
        protected override Filter GetFilter(IEntity entity)
        {
            return Filter.Local();
        }
    }
}
