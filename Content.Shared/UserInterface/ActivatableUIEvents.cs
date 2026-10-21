namespace Content.Shared.UserInterface;

/// <summary>
/// Raised on the entity with an activatable UI when attempting to open it.
/// This is raised BEFORE opening a UI! Do not listen and then open / do something use
/// <see cref="AfterActivatableUIOpenEvent"/> for that.
/// </summary>
public sealed class ActivatableUIOpenAttemptEvent(EntityUid user, bool silent) : CancellableEntityEventArgs
{
    /// <summary>
    /// The player trying to open the UI.
    /// </summary>
    public readonly EntityUid User = user;

    /// <summary>
    /// Whether subscriptions are allowed to play a sound or show popups.
    /// This is used to prevent just looking at the verb without even clicking on it showing a popup or playing sounds.
    /// </summary>
    public bool Silent = silent;
}

/// <summary>
/// Raised on the player when they are attempting to open an activatable UI.
/// This is raised BEFORE opening a UI! Do not listen and then open / do something use
/// <see cref="AfterActivatableUIOpenEvent"/> for that.
/// </summary>
public sealed class UserOpenActivatableUIAttemptEvent(EntityUid user, EntityUid target, bool silent) : CancellableEntityEventArgs //have to one-up the already stroke-inducing name
{
    /// <summary>
    /// The player trying to open the UI.
    /// </summary>
    public readonly EntityUid User = user;

    /// <summary>
    /// The target entity with the UI.
    /// </summary>
    public readonly EntityUid Target = target;

    /// <summary>
    /// Whether subscriptions are allowed to play a sound or show popups.
    /// This is used to prevent just looking at the verb without even clicking on it to show a popup or play sounds if the attempt is cancelled.
    /// </summary>
    public bool Silent = silent;
}

/// <summary>
/// Raised on the entity with an activatable UI after the UI has been opened.
/// </summary>
public sealed class AfterActivatableUIOpenEvent(EntityUid user) : EntityEventArgs
{
    /// <summary>
    /// The player that opened the UI.
    /// </summary>
    public readonly EntityUid User = user;
}

/// <summary>
/// Raised on the entity with an activatable UI after it's decided the user can open the UI,
/// but before the UI actually opens.
/// Use this if you need to prepare the UI itself.
/// </summary>
public sealed class BeforeActivatableUIOpenEvent(EntityUid user) : EntityEventArgs
{
    /// <summary>
    /// The player that is opening the UI.
    /// </summary>
    public readonly EntityUid User = user;
}

public sealed class ActivatableUIPlayerChangedEvent : EntityEventArgs
{
}
