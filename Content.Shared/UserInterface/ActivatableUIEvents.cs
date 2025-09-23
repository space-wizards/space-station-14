namespace Content.Shared.UserInterface;

/// <summary>
/// This is raised BEFORE opening a UI! Do not listen and then open / do something use
/// <see cref="AfterActivatableUIOpenEvent"/> for that.
/// </summary>
public sealed class ActivatableUIOpenAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public ActivatableUIOpenAttemptEvent(EntityUid who)
    {
        User = who;
    }
}

/// <summary>
/// Event raised on the entity itself before adding verb, which can be canceled to stop adding the verb.
/// </summary>
public sealed class ActivatableUIAddVerbAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public ActivatableUIAddVerbAttemptEvent(EntityUid who) => User = who;
}

public sealed class UserOpenActivatableUIAttemptEvent : CancellableEntityEventArgs //have to one-up the already stroke-inducing name
{
    public EntityUid User { get; }
    public EntityUid Target { get; }
    public UserOpenActivatableUIAttemptEvent(EntityUid who, EntityUid target)
    {
        User = who;
        Target = target;
    }
}
/// <summary>
/// Raised on the user when trying adding verbs, can be canceled and not show verbs.
/// </summary>
public sealed class UserAddVerbActivatableUIAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public EntityUid Target { get; }
    public UserAddVerbActivatableUIAttemptEvent(EntityUid who, EntityUid target)
    {
        User = who;
        Target = target;
    }
}

public sealed class AfterActivatableUIOpenEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public readonly EntityUid Actor;

    public AfterActivatableUIOpenEvent(EntityUid who, EntityUid actor)
    {
        User = who;
        Actor = actor;
    }
}

/// <summary>
/// This is after it's decided the user can open the UI,
/// but before the UI actually opens.
/// Use this if you need to prepare the UI itself
/// </summary>
public sealed class BeforeActivatableUIOpenEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public BeforeActivatableUIOpenEvent(EntityUid who)
    {
        User = who;
    }
}

public sealed class ActivatableUIPlayerChangedEvent : EntityEventArgs
{
}
