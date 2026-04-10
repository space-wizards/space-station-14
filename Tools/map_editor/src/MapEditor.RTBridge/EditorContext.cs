using System;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Log;

namespace MapEditor.RTBridge;

/// <summary>
///     The bridge between the WPF host thread and the RT game thread.
/// </summary>
/// <remarks>
///     Commands initiated from the WPF UI (menu clicks, buttons, etc.) call
///     methods on this context from the host thread. The context marshals
///     them onto the RT game thread via <see cref="ITaskManager.RunOnMainThread"/>
///     where they can safely touch RT systems (EntityManager, MapSystem,
///     prototype manager).
///
///     <para>
///     Methods return <see cref="Task"/> so the host can await the result on
///     the UI thread. The continuation runs back on whatever sync context
///     the caller had, which is the WPF dispatcher by default, so code after
///     an await can safely touch WPF controls.
///     </para>
/// </remarks>
public sealed class EditorContext
{
    /// <summary>
    ///     Shared singleton set up by <see cref="EditorBootstrap"/> during the
    ///     PostInitCallback. Null until RT has finished initializing.
    /// </summary>
    public static EditorContext? Current { get; internal set; }

    /// <summary>
    ///     Completes exactly once when <see cref="Current"/> has been
    ///     published. Host code can <c>await EditorContext.Ready</c> to get
    ///     a handle to the context without polling or racing on the static.
    /// </summary>
    public static Task<EditorContext> Ready => _readyTcs.Task;

    private static readonly TaskCompletionSource<EditorContext> _readyTcs
        = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static void PublishReady(EditorContext context)
    {
        Current = context;
        _readyTcs.TrySetResult(context);
    }

    private readonly ITaskManager _taskManager;
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly ISawmill _sawmill;

    /// <summary>
    ///     The editor's current eye. Set up during post init. Mutations to
    ///     <see cref="Eye.Position"/> or <see cref="Eye.Zoom"/> are picked
    ///     up by the viewport on the next frame.
    /// </summary>
    public Eye EditorEye { get; internal set; } = default!;

    internal EditorContext(
        ITaskManager taskManager,
        IEntityManager entityManager,
        IEyeManager eyeManager)
    {
        _taskManager = taskManager;
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        _sawmill = Logger.GetSawmill("map_editor");
    }

    /// <summary>
    ///     Queue a callback to run on the RT game thread. Returns a task
    ///     that completes (or faults) when the callback has finished.
    /// </summary>
    public Task RunOnGameThread(Action callback)
    {
        var tcs = new TaskCompletionSource();
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                callback();
                tcs.SetResult();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        });
        return tcs.Task;
    }
}
