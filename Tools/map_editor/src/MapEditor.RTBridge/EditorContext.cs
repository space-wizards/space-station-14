using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

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

    /// <summary>
    ///     Loads an SS14 map YAML from an absolute disk path and points
    ///     the editor eye at the newly loaded map. Safe to call from any
    ///     thread, the actual work runs on the game thread.
    /// </summary>
    public Task LoadMapAsync(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            throw new ArgumentException("Path must not be empty", nameof(absolutePath));

        return RunOnGameThread(() => LoadMapInternal(absolutePath));
    }

    private void LoadMapInternal(string absolutePath)
    {
        _sawmill.Info($"LoadMap: {absolutePath}");

        if (!File.Exists(absolutePath))
            throw new FileNotFoundException("Map file not found", absolutePath);

        var mapLoader = _entityManager.System<MapLoaderSystem>();

        // TryLoadGeneric with a stream lets us load maps from anywhere on
        // disk, not just under Resources/.
        using var stream = File.OpenRead(absolutePath);
        var fileName = Path.GetFileName(absolutePath);

        if (!mapLoader.TryLoadGeneric(stream, fileName, out var loadResult))
            throw new InvalidOperationException($"Failed to load map: {absolutePath}");

        _sawmill.Info(
            $"LoadMap: loaded {loadResult.Maps.Count} map(s) and {loadResult.Grids.Count} grid(s)");

        // Point the editor eye at the loaded map. Prefer an explicit map
        // from the file, fall back to an orphan grid's auto created map.
        MapId? targetMap = null;
        foreach (var mapEnt in loadResult.Maps)
        {
            targetMap = _entityManager.GetComponent<MapComponent>(mapEnt.Owner).MapId;
            break;
        }
        if (targetMap == null)
        {
            foreach (var gridEnt in loadResult.Grids)
            {
                targetMap = _entityManager.GetComponent<TransformComponent>(gridEnt.Owner).MapID;
                break;
            }
        }
        if (targetMap == null)
        {
            _sawmill.Warning("LoadMap: file produced no maps or grids");
            return;
        }

        EditorEye.Position = new MapCoordinates(new Vector2(1.5f, 1.5f), targetMap.Value);
        _eyeManager.CurrentEye = EditorEye;
        _sawmill.Info($"LoadMap: editor eye switched to map {targetMap.Value}");
    }
}
