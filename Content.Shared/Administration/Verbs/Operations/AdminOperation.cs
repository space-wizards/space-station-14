namespace Content.Shared.Administration.Verbs.Operations;

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

public interface IAdminOperationRaiser
{
    void RaiseOperationEvent<T>(EntityUid target, EntityUid user, T operation) where T : AdminOperationBase<T>;
}

[ByRefEvent]
public readonly record struct AdminOperationEvent<T>(T Operation, EntityUid User) where T : AdminOperationBase<T>;
