namespace Content.Shared.Gravity
{
    [ByRefEvent]
    public readonly record  struct GravityChangedEvent(EntityUid ChangedGridIndex, bool HasGravity);
}
