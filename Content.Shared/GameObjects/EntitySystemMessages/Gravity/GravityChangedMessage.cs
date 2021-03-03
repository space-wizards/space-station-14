#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.GameObjects.EntitySystemMessages.Gravity
{
    public class GravityChangedMessage : EntitySystemMessage
    {
        public GravityChangedMessage(IMapGrid grid)
        {
            Grid = grid;
        }

        public IMapGrid Grid { get; }

        public bool HasGravity => Grid.HasGravity;
    }
}
