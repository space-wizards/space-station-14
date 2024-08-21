using Content.Shared.Chemistry.Components.Solutions;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    private readonly Dictionary<Type, Delegate> _wrappedEvents = new();

    protected void RaiseLocalSolutionEvent<T>(Entity<SolutionComponent> solution,
        T eventArgs,
        bool raiseParent = true,
        bool broadcast = false
    ) where T: notnull
    {
        RaiseLocalEvent(solution, eventArgs, broadcast);
        if (raiseParent && solution.Comp.Container != EntityUid.Invalid)
        {
            RaiseLocalEvent(solution.Comp.Container, eventArgs, broadcast);
        }
    }
    protected void RaiseLocalSolutionEvent<T>(Entity<SolutionComponent> solution,
        ref T eventArgs,
        bool raiseParent = true,
        bool broadcast = false
    ) where T: notnull
    {
        RaiseLocalEvent(solution, ref eventArgs, broadcast);
        if (raiseParent && solution.Comp.Container != EntityUid.Invalid)
        {
            RaiseLocalEvent(solution.Comp.Container, ref eventArgs, broadcast);
        }
    }

    protected void SubscribeRelayedEvent<T>(WrappedSolutionEvent<T> handler) where T: notnull
    {
        SubscribeLocalEvent<SolutionHolderComponent, T>(RelayedEventHandler);
        if (_wrappedEvents.TryAdd(typeof(T), handler))
            return;
        Log.Error($"Duplicate subscribe for event:{typeof(T)}");
    }

    private void RelayedEventHandler<T>(Entity<SolutionHolderComponent> solutionHolder, ref T args) where T: notnull
    {
        var handler = _wrappedEvents[typeof(T)] as WrappedSolutionEvent<T>;
        foreach (var solution in EnumerateSolutions((solutionHolder,solutionHolder)))
        {
            handler?.Invoke(solution, ref args);
        }
    }

    [UsedImplicitly]
    protected delegate void WrappedSolutionEvent<T>(Entity<SolutionComponent> solution,ref T eventArgs) where T: notnull;
}
