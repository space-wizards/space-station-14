using Robust.Shared.Physics.Components;

namespace Content.Shared.Movement.Pulling.Events;

public abstract class PullMessage : EntityEventArgs
{
    public readonly PhysicsComponent Puller;
    public readonly PhysicsComponent Pulled;

    protected PullMessage(PhysicsComponent puller, PhysicsComponent pulled)
    {
        Puller = puller;
        Pulled = pulled;
    }
}
