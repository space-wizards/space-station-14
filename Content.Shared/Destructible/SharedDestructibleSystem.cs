namespace Content.Shared.Destructible;

public abstract class SharedDestructibleSystem : EntitySystem
{
    /// <summary>
    ///     Force entity to be destroyed and deleted.
    /// </summary>
    public void DestroyEntity(EntityUid owner, EntityUid? gatherTool = null, EntityUid? gatherUser = null)
    {
        var eventArgs = new DestructionEventArgs(gatherTool, gatherUser);

        RaiseLocalEvent(owner, eventArgs);
        QueueDel(owner);
    }

    /// <summary>
    ///     Force entity to broke.
    /// </summary>
    public void BreakEntity(EntityUid owner)
    {
        var eventArgs = new BreakageEventArgs();
        RaiseLocalEvent(owner, eventArgs);
    }
}

/// <summary>
///     Raised when entity is destroyed and about to be deleted.
/// </summary>
public sealed class DestructionEventArgs : EntityEventArgs
{
    public DestructionEventArgs(EntityUid? gatherTool, EntityUid? gatherUser)
    {
        GatherTool = gatherTool;
        GatherUser = gatherUser;
    }

    public EntityUid? GatherTool { get; }
    public EntityUid? GatherUser { get; }
}

/// <summary>
///     Raised when entity was heavy damage and about to break.
/// </summary>
public sealed class BreakageEventArgs : EntityEventArgs
{
}
