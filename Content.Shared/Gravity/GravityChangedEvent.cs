namespace Content.Shared.Gravity
{
    public sealed class GravityChangedEvent : EntityEventArgs
    {
        public GravityChangedEvent(EntityUid changedGridIndex, bool newGravityState)
        {
            HasGravity = newGravityState;
            ChangedGridIndex = changedGridIndex;
        }

        public EntityUid ChangedGridIndex { get; }

        public bool HasGravity { get; }
    }
}
