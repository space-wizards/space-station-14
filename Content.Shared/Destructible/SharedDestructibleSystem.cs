namespace Content.Shared.Destructible;

public abstract class SharedDestructibleSystem : EntitySystem
{
    /// <summary>
    ///     Force entity to be destroyed and deleted.
    /// </summary>
    public void DestroyEntity(EntityUid owner)
    {
        var eventArgs = new DestructionEventArgs();

        RaiseLocalEvent(owner, eventArgs, false);
        QueueDel(owner);
    }
}

public sealed class DestructionEventArgs : EntityEventArgs
{

}
