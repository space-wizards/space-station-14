namespace Content.Shared.Power.Events;

/// <summary>
///     Event to be raised when a cable is anchored / unanchored
/// </summary>
[ByRefEvent]
public readonly struct CableAnchorStateChangedEvent
{
    public readonly Entity<TransformComponent> Xform;
    public EntityUid Entity => Xform.Owner;
    public bool Anchored => Xform.Comp.Anchored;

    /// <summary>
    ///     If true, the entity is being detached to null-space
    /// </summary>
    public readonly bool Detaching;

    public CableAnchorStateChangedEvent(Entity<TransformComponent> xform, bool detaching = false)
    {
        Detaching = detaching;
        Xform = xform;
    }
}
