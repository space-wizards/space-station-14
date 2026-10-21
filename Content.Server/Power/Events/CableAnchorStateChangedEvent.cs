namespace Content.Server.Power.Events;

/// <summary>
/// Event to be raised when a cable is anchored / unanchored
/// </summary>
[ByRefEvent]
public readonly struct CableAnchorStateChangedEvent(Entity<TransformComponent> cable, bool detaching = false)
{
    public readonly Entity<TransformComponent> Cable = cable;
    public bool Anchored => Cable.Comp.Anchored;

    /// <summary>
    /// If true, the entity is being detached to null-space
    /// </summary>
    public readonly bool Detaching = detaching;
}
