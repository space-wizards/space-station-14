#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Gravity
{
    public class GravityChangedMessage : EntityEventArgs
    {
        public GravityChangedMessage(GridId changedGridIndex, bool newGravityState)
        {
            HasGravity = newGravityState;
            ChangedGridIndex = changedGridIndex;
        }

        public GridId ChangedGridIndex { get; }

        public bool HasGravity { get; }
    }
}
