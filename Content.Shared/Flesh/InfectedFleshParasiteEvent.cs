using Content.Shared.Actions;

namespace Content.Shared.Flesh;

public readonly struct EntityInfectedFleshParasiteEvent
{
    public readonly EntityUid Target;

    public EntityInfectedFleshParasiteEvent(EntityUid target)
    {
        Target = target;
    }
};

public sealed class ZombifySelfActionEvent : InstantActionEvent { };
