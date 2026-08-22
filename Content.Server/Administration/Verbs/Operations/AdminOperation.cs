namespace Content.Server.Administration.Verbs.Operations;

/// <summary>
/// A data-defined step in an admin verb, dispatched to the target as a local event.
/// If the target does not match an operation's handler, the operation has no effect.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AdminOperation
{
    public abstract void RaiseEvent(EntityUid target, EntityUid user, AdminOperationSystem system);
}

/// <summary>
/// Gives each operation a strongly typed local event without reflection.
/// </summary>
public abstract partial class AdminOperationBase<T> : AdminOperation where T : AdminOperationBase<T>
{
    public override void RaiseEvent(EntityUid target, EntityUid user, AdminOperationSystem system)
    {
        system.RaiseOperationEvent(target, user, (T) this);
    }
}

/// <summary>
/// Carries a data-defined operation and the user entity that invoked it.
/// </summary>
[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
