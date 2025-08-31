using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._NullLink.Helpers;
#pragma warning disable RA0004 // Risk of deadlock from accessing Task<T>.Result

public static class Pipe
{
    /// <summary>
    ///     Off-loads the supplied asynchronous work to the ThreadPool immediately,
    ///     so the caller returns to the main thread without awaiting.  
    ///     All exceptions are caught and routed to <paramref name="onError"/> (if given)
    ///     and to <see cref="Debug.WriteLine"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void RunInBackground(
        Func<Task> work,
        Action<Exception>? onError = null) 
        => ThreadPool.UnsafeQueueUserWorkItem(
            static async state =>
            {
                var (job, handler) = ((Func<Task> job, Action<Exception>? handler))state!;
                try
                {
                    await job().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    handler?.Invoke(ex);
                    Debug.WriteLine(ex);
                }
            },
            (work, onError));

    /// <summary>
    ///     Overload for <see cref="ValueTask"/> producers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void RunInBackgroundVT(
        Func<ValueTask> work,
        Action<Exception>? onError = null)
        => RunInBackground(() => work().AsTask(), onError);

    public static void FireAndForget(this Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                Debug.WriteLine(t.Exception);
        },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.DenyChildAttach,
        TaskScheduler.Default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void FireAndForget(
        this Task task,
        Action<Exception>? onError = null)
    {
        if (task.IsCompletedSuccessfully || task.IsCanceled) return;

        task.ContinueWith(
            static (t, state) =>
            {
                var handler = (Action<Exception>?)state;
                handler?.Invoke(t.Exception!.Flatten());
                Debug.WriteLine(t.Exception);
            },
            onError,
            TaskContinuationOptions.ExecuteSynchronously |
            TaskContinuationOptions.DenyChildAttach |
            TaskContinuationOptions.OnlyOnFaulted);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void FireAndForget(this ValueTask valueTask)
    {
        if (valueTask.IsCompleted) return;
        valueTask.AsTask().FireAndForget();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static void FireAndForget(this ValueTask valueTask, Action<Exception>? onError = null)
    {
        if (valueTask.IsCompletedSuccessfully || valueTask.IsCanceled) return;
        valueTask.AsTask().FireAndForget(onError);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static Task<T2> Then<T, T2>(this Task<T> task, Func<T, T2> func)
        => task.ContinueWith(completedTask => completedTask.IsFaulted
            ? throw completedTask.Exception.InnerException ?? new Exception("Task was faulted, but the exception is null")
            : func(completedTask.Result), TaskContinuationOptions.NotOnCanceled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static Task<T2> Then<T, T2>(this Task<T> task, Func<T, Task<T2>> func)
        => task.ContinueWith(completedTask => completedTask.IsFaulted
            ? throw completedTask.Exception.InnerException ?? new Exception("Task was faulted, but the exception is null")
            : func(completedTask.Result), TaskContinuationOptions.NotOnCanceled).Unwrap();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static Task<T2> Then<T, T2>(this Task<T> task, Func<T, ValueTask<T2>> func)
        => task.ContinueWith(completedTask => completedTask.IsFaulted
            ? throw completedTask.Exception.InnerException ?? new Exception("Task was faulted, but the exception is null")
            : func(completedTask.Result).AsTask(), TaskContinuationOptions.NotOnCanceled).Unwrap();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static Task Then<T>(this Task<T> task, Action<T> action)
        => task.ContinueWith(completedTask =>
        {
            if (completedTask.IsFaulted)
                throw completedTask.Exception.InnerException ?? new Exception("Task was faulted, but the exception is null");
            action(completedTask.Result);
        }, TaskContinuationOptions.NotOnCanceled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static Task Then(this Task task, Action action)
        => task.ContinueWith(completedTask =>
        {
            if (completedTask.IsFaulted)
                throw completedTask.Exception.InnerException ?? new Exception("Task was faulted, but the exception is null");
            action();
        }, TaskContinuationOptions.NotOnCanceled);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static ValueTask<T2> Then<T, T2>(this ValueTask<T> task, Func<T, T2> func)
        => task.IsCompleted ? ValueTask.FromResult(func(task.Result))
            : new ValueTask<T2>(task.AsTask().Then(func));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static ValueTask Then<T>(this ValueTask<T> task, Func<T, ValueTask> func)
        => task.IsCompleted ? func(task.Result)
            : new ValueTask(task.AsTask().Then(func));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static ValueTask<T2> Then<T, T2>(this ValueTask<T> task, Func<T, ValueTask<T2>> func)
        => task.IsCompleted ? func(task.Result)
            : new ValueTask<T2>(task.AsTask().Then(func));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static ValueTask Then<T>(this ValueTask<T> task, Action<T> action)
    {
        if (task.IsCompleted)
        {
            action(task.Result);
            return ValueTask.CompletedTask;
        }
        return new ValueTask(task.AsTask().Then(action));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    [DebuggerHidden]
    public static ValueTask Then(this ValueTask task, Action action)
    {
        if (task.IsCompleted)
        {
            action();
            return ValueTask.CompletedTask;
        }
        return new ValueTask(task.AsTask().Then(action));
    }
}