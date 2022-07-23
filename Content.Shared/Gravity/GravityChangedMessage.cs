using Robust.Shared.Map;

namespace Content.Shared.Gravity
{
    public sealed class GravityChangedMessage : EntityEventArgs
    {
        public GravityChangedMessage(EntityUid changedGridIndex, bool newGravityState)
        {
            HasGravity = newGravityState;
            ChangedGridIndex = changedGridIndex;
        }

        public EntityUid ChangedGridIndex { get; }

        public bool HasGravity { get; }
    }
}
