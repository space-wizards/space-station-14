namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// A data-defined step in an admin verb, dispatched to the target as a local event.
/// If the target does not match an operation's handler, the operation has no effect.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AdminOperation
{
    public abstract void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser);
}

/// <summary>
/// Gives each operation a strongly typed local event without reflection.
/// </summary>
public abstract partial class AdminOperationBase<T> : AdminOperation where T : AdminOperationBase<T>
{
    public override void RaiseEvent(EntityUid target, EntityUid user, IAdminOperationRaiser raiser)
    {
        raiser.RaiseOperationEvent(target, user, (T) this);
    }
}

/// <summary>
/// Provides the dispatch bridge used by operations to raise their strongly typed local events.
/// </summary>
public interface IAdminOperationRaiser
{
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>;
}

/// <summary>
/// Carries a data-defined operation and the user entity that invoked it.
/// </summary>
[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
