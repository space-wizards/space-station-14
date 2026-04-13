using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

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
    ///     The editor's current eye. Set up during post init.
    ///     <para>
    ///     IMPORTANT: do NOT mutate <see cref="Eye.Position"/> or
    ///     <see cref="Eye.Zoom"/> directly to pan/zoom from host code.
    ///     The viewport's <c>FrameUpdate</c> writes the editor camera's
    ///     state back to this eye every frame, so direct edits get
    ///     overwritten on the next tick. Mutate <see cref="Camera"/>
    ///     instead.
    ///     </para>
    /// </summary>
    public Eye EditorEye { get; internal set; } = default!;

    /// <summary>
    ///     Toggle lighting (DrawLight + DrawFov) on the editor eye.
    ///     Must be called from the WPF thread — marshals to the game thread.
    /// </summary>
    public void SetLighting(bool enabled)
    {
        RunOnGameThread(() =>
        {
            EditorEye.DrawLight = enabled;
            EditorEye.DrawFov = enabled;
        });
    }

    /// <summary>
    ///     The editor camera. This is the source of truth for viewport
    ///     position and zoom: the viewport's <c>FrameUpdate</c> reads
    ///     from here and writes back to the eye every frame, so
    ///     mutations land on the next rendered frame.
    ///
    ///     Published by <see cref="EditorViewportControl"/> from its
    ///     constructor. Null until the viewport state has started up.
    /// </summary>
    public EditorCamera? Camera { get; internal set; }

    /// <summary>
    ///     Prototype id of the entity that should be spawned when the user
    ///     left clicks the viewport. Null means "left click does nothing".
    ///     Written by the WPF host when the user picks an entity in the
    ///     palette, read by <see cref="EditorViewportControl"/> on every
    ///     left click. Reference reads and writes are atomic in .NET, so
    ///     this does not need a lock.
    /// </summary>
    public string? PlacementPrototypeId { get; set; }

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

    /// <summary>
    ///     Returns the full list of spawnable entity prototypes the user
    ///     can place from the editor palette. Runs on the game thread
    ///     because <see cref="IPrototypeManager"/> is not guaranteed
    ///     thread safe, then hands the list back to the caller.
    /// </summary>
    public Task<IReadOnlyList<SpawnablePrototype>> GetSpawnablePrototypesAsync()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<SpawnablePrototype>>();
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                var list = new List<SpawnablePrototype>();
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!IsSpawnable(
                            isAbstract: proto.Abstract,
                            hideSpawnMenu: proto.HideSpawnMenu,
                            hasSprite: proto.Components.ContainsKey("Sprite")))
                        continue;

                    list.Add(new SpawnablePrototype(proto.ID, proto.Name));
                }
                list.Sort(static (a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                _sawmill.Info($"GetSpawnablePrototypes: {list.Count} entries");
                tcs.SetResult(list);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    ///     Pure predicate that decides whether an entity prototype should
    ///     appear in the spawn palette. Broken out from the enumeration
    ///     path so it can be unit tested without having to stand up an
    ///     <see cref="IPrototypeManager"/>.
    /// </summary>
    /// <param name="isAbstract">
    ///     <see cref="EntityPrototype.Abstract"/>. Abstract bases exist
    ///     only for inheritance and should never be placed directly.
    /// </param>
    /// <param name="hideSpawnMenu">
    ///     <see cref="EntityPrototype.HideSpawnMenu"/>. Content explicitly
    ///     opts out of spawn menus for things like map markers, runtime
    ///     only entities, debugging helpers.
    /// </param>
    /// <param name="hasSprite">
    ///     True if the prototype's component registry contains a
    ///     <c>Sprite</c> component. Without one there is nothing visible
    ///     to place, so the entity has no business in a visual palette.
    /// </param>
    public static bool IsSpawnable(bool isAbstract, bool hideSpawnMenu, bool hasSprite)
    {
        if (isAbstract)
            return false;
        if (hideSpawnMenu)
            return false;
        if (!hasSprite)
            return false;
        return true;
    }

    // ---- Benchmark ----

    /// <summary>
    ///     How long each phase of the benchmark samples for, in
    ///     milliseconds. Long enough to capture a stable rolling average,
    ///     short enough that the whole run finishes in a few seconds.
    /// </summary>
    private const int PhaseSampleMs = 600;

    /// <summary>
    ///     How long to wait after applying a camera change before sampling
    ///     starts, so transient state (frame allocations, dirty lookups,
    ///     etc.) does not pollute the average.
    /// </summary>
    private const int PhaseSettleMs = 250;

    /// <summary>
    ///     Sampling interval inside a phase. About one wpf timer tick.
    /// </summary>
    private const int SampleIntervalMs = 16;

    /// <summary>
    ///     Run a fixed sequence of camera mutations and sample render
    ///     metrics during each. Phases:
    ///
    ///     <list type="number">
    ///     <item>baseline: whatever the user had set up before clicking</item>
    ///     <item>fit-to-map: zoom out to encompass every grid on the active map</item>
    ///     <item>wide stable: hold the wide shot for the sample window</item>
    ///     <item>pan: walk through 4 waypoints around the map bounds</item>
    ///     <item>zoom in 2x: half the wide-shot zoom factor</item>
    ///     <item>zoom out 2x: double the wide-shot zoom factor</item>
    ///     <item>restore: back to the baseline view</item>
    ///     </list>
    ///
    ///     The mutations run on the game thread, but sampling happens in
    ///     a wall clock loop that posts <see cref="RunOnGameThread"/>
    ///     calls between Task.Delay yields. That way each sample is taken
    ///     during a real RT frame and the host thread does not block.
    /// </summary>
    public async Task<BenchmarkResult> RunBenchmarkAsync()
    {
        _sawmill.Info("Benchmark: starting");

        // The viewport publishes its EditorCamera through Camera. We
        // mutate THAT, not EditorEye directly, because the viewport's
        // FrameUpdate writes camera state back to the eye every tick
        // and would clobber any direct eye edits.
        if (Camera == null)
            throw new InvalidOperationException(
                "EditorContext.Camera is null. The viewport state must be active before " +
                "the benchmark can run.");

        // Snapshot the baseline view so we can restore at the end and so
        // every phase has a known reference. Read on the game thread to
        // avoid racing the renderer.
        Vector2 baselinePos = default;
        Vector2 baselineZoom = default;
        MapId baselineMap = default;
        await RunOnGameThread(() =>
        {
            baselinePos = Camera.Position;
            baselineZoom = Camera.Zoom;
            baselineMap = EditorEye.Position.MapId;
        });

        // Compute the union AABB of all grids on the active map. This
        // becomes the wide shot framing.
        Box2 mapBounds = default;
        var hasBounds = false;
        await RunOnGameThread(() =>
        {
            (mapBounds, hasBounds) = ComputeMapBounds(baselineMap);
        });

        if (!hasBounds)
        {
            _sawmill.Warning("Benchmark: no grids on active map, cannot compute fit to map");
            // Fall back: still run the benchmark but use a fixed wide shot.
            mapBounds = Box2.UnitCentered.Scale(40f); // 40x40 tile box centered on origin
        }

        // Translate the map AABB into a camera position + zoom that fits.
        var (wideCenter, wideZoom) = ComputeFitCameraForBounds(mapBounds);

        var phases = new List<BenchmarkPhase>();

        // Phase 1: baseline (wherever the user left the camera).
        phases.Add(await SamplePhaseAsync("baseline", PhaseSettleMs, PhaseSampleMs));

        // Phase 2: fit to map — zoom just enough to see the full map.
        await RunOnGameThread(() =>
        {
            Camera.Position = wideCenter;
            Camera.Zoom = wideZoom;
        });
        phases.Add(await SamplePhaseAsync("wide (fit-to-map)", PhaseSettleMs, PhaseSampleMs));

        // Capture the visible entity count at the wide shot.
        var visibleAtWide = 0;
        await RunOnGameThread(() =>
        {
            visibleAtWide = CountVisibleEntities(baselineMap, wideCenter, wideZoom);
        });

        // Phase 3: zoom in to 50% of wide (half the map visible) and
        // pan across 4 waypoints to simulate dragging around.
        var midZoom = wideZoom * 0.5f;
        var panRadius = MathF.Max(mapBounds.Width, mapBounds.Height) * 0.15f;
        var waypoints = new[]
        {
            wideCenter + new Vector2(panRadius, 0),
            wideCenter + new Vector2(0, panRadius),
            wideCenter + new Vector2(-panRadius, 0),
            wideCenter + new Vector2(0, -panRadius),
        };
        await RunOnGameThread(() =>
        {
            Camera.Zoom = midZoom;
            Camera.Position = waypoints[0];
        });
        phases.Add(await SamplePhaseAsync("mid-zoom pan 1/4", PhaseSettleMs, PhaseSampleMs));

        for (var i = 1; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            await RunOnGameThread(() => Camera.Position = wp);
            phases.Add(await SamplePhaseAsync($"mid-zoom pan {i + 1}/{waypoints.Length}", PhaseSettleMs, PhaseSampleMs));
        }

        // Phase 7: zoom in to 25% of wide (close-up, ~quarter of map).
        await RunOnGameThread(() =>
        {
            Camera.Position = wideCenter;
            Camera.Zoom = wideZoom * 0.25f;
        });
        phases.Add(await SamplePhaseAsync("close-up (25%)", PhaseSettleMs, PhaseSampleMs));

        // Phase 8: back to wide shot to bookend.
        await RunOnGameThread(() =>
        {
            Camera.Zoom = wideZoom;
        });
        phases.Add(await SamplePhaseAsync("wide (return)", PhaseSettleMs, PhaseSampleMs));

        // A/B comparison: disable the fast path and re-run the wide shot
        // so we can see the difference in the same benchmark report.
        await RunOnGameThread(() =>
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.RenderSpriteSimpleFastPath, false);
            Camera.Position = wideCenter;
            Camera.Zoom = wideZoom;
        });
        phases.Add(await SamplePhaseAsync("wide (fast path OFF)", PhaseSettleMs, PhaseSampleMs));

        // Re-enable sprite fast path.
        await RunOnGameThread(() =>
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.RenderSpriteSimpleFastPath, true);
        });

        // A/B comparison: enable LOD culling at 4px threshold.
        await RunOnGameThread(() =>
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.RenderMinSpriteSize, 4f);
            Camera.Position = wideCenter;
            Camera.Zoom = wideZoom;
        });
        phases.Add(await SamplePhaseAsync("wide (LOD 4px)", PhaseSettleMs, PhaseSampleMs));

        // LOD culling at 2px threshold.
        await RunOnGameThread(() =>
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.RenderMinSpriteSize, 2f);
        });
        phases.Add(await SamplePhaseAsync("wide (LOD 2px)", PhaseSettleMs, PhaseSampleMs));

        // Disable LOD and restore.
        await RunOnGameThread(() =>
        {
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.RenderMinSpriteSize, 0f);
            Camera.Position = baselinePos;
            Camera.Zoom = baselineZoom;
        });

        var result = new BenchmarkResult(
            MapLabel: $"map {baselineMap}, bounds {mapBounds.Size}",
            VisibleEntityCountAtWideShot: visibleAtWide,
            Phases: phases);

        _sawmill.Info("Benchmark: done\n" + result.FormatReport());
        return result;
    }

    /// <summary>
    ///     Compute the union AABB of every grid on the given map. Returns
    ///     (default, false) when there are no grids.
    /// </summary>
    private (Box2 bounds, bool hasAny) ComputeMapBounds(MapId mapId)
    {
        Box2? union = null;
        var query = _entityManager.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();
        var transformSys = _entityManager.System<SharedTransformSystem>();
        while (query.MoveNext(out var uid, out var grid, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var worldMatrix = transformSys.GetWorldMatrix(uid);
            var worldAabb = worldMatrix.TransformBox(grid.LocalAABB);
            union = union == null ? worldAabb : union.Value.Union(worldAabb);
        }
        return union.HasValue ? (union.Value, true) : (default, false);
    }

    /// <summary>
    ///     Given a world space AABB, compute a camera (Position, Zoom)
    ///     that frames the box centered in the viewport with a small
    ///     margin. Assumes the editor's viewport is roughly 980x720
    ///     virtual pixels (matches the WPF window setup).
    /// </summary>
    private static (Vector2 center, Vector2 zoom) ComputeFitCameraForBounds(Box2 bounds)
    {
        const float pixelsPerTile = 32f;
        const float marginFactor = 1.1f;
        const float viewportW = 980f;
        const float viewportH = 720f;

        var center = bounds.Center;
        var halfW = bounds.Width / 2f * marginFactor;
        var halfH = bounds.Height / 2f * marginFactor;

        // ScreenToWorld in EditorCamera is: world = position + (centeredPx * zoom / pixelsPerTile)
        // Solving for zoom such that the viewport edge maps to the AABB
        // edge: zoom = halfBounds * pixelsPerTile / halfViewport.
        var zoomX = halfW * pixelsPerTile / (viewportW / 2f);
        var zoomY = halfH * pixelsPerTile / (viewportH / 2f);
        var zoom = MathF.Max(zoomX, zoomY);
        return (center, new Vector2(zoom));
    }

    /// <summary>
    ///     Approximate count of how many sprite component entities are
    ///     visible from a given camera. Uses the same sprite tree query
    ///     RT's renderer uses, so the number matches what would actually
    ///     end up in <c>_drawingSpriteList</c>.
    /// </summary>
    private int CountVisibleEntities(MapId mapId, Vector2 center, Vector2 zoom)
    {
        const float pixelsPerTile = 32f;
        const float viewportW = 980f;
        const float viewportH = 720f;
        var halfW = viewportW / 2f * zoom.X / pixelsPerTile;
        var halfH = viewportH / 2f * zoom.Y / pixelsPerTile;
        var aabb = Box2.CenteredAround(center, new Vector2(halfW * 2f, halfH * 2f));

        var count = 0;
        var query = _entityManager.AllEntityQueryEnumerator<SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var _, out var sprite, out var xform))
        {
            if (xform.MapID != mapId || !sprite.Visible)
                continue;
            if (aabb.Contains(xform.WorldPosition))
                count++;
        }
        return count;
    }

    /// <summary>
    ///     Sample render metrics for the configured window. Sleeps a
    ///     settle period first, then samples once per <see cref="SampleIntervalMs"/>
    ///     until the window expires, returning the averages.
    /// </summary>
    private async Task<BenchmarkPhase> SamplePhaseAsync(string name, int settleMs, int sampleMs)
    {
        await Task.Delay(settleMs);

        var fpsTotal = 0d;
        var ftTotal = 0d;
        var ftMax = 0d;
        var clyTotal = 0d;
        var glTotal = 0d;
        var batchTotal = 0d;
        var largestVTotal = 0d;
        var simpleTotal = 0L;
        var fullTotal = 0L;
        var samples = 0;

        var deadline = DateTime.UtcNow.AddMilliseconds(sampleMs);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(SampleIntervalMs);
            await RunOnGameThread(() =>
            {
                var clyde = IoCManager.Resolve<IClyde>();
                var timing = IoCManager.Resolve<IGameTiming>();
                var stats = clyde.DebugStats;
                var spriteSystem = _entityManager.System<SpriteSystem>();

                fpsTotal += timing.FramesPerSecondAvg;
                var ft = timing.RealFrameTime.TotalMilliseconds;
                ftTotal += ft;
                if (ft > ftMax) ftMax = ft;
                clyTotal += stats.LastClydeDrawCalls;
                glTotal += stats.LastGLDrawCalls;
                batchTotal += stats.LastBatches;
                largestVTotal += stats.LargestBatchSize.vertices;
                simpleTotal += spriteSystem.SimpleSpriteCount;
                fullTotal += spriteSystem.FullSpriteCount;
                samples++;
            });
        }

        if (samples == 0)
            return new BenchmarkPhase(name, 0, 0, 0, 0, 0, 0, 0, 0);

        return new BenchmarkPhase(
            Name: name,
            SampleCount: samples,
            FpsAvg: fpsTotal / samples,
            FrameTimeMsAvg: ftTotal / samples,
            FrameTimeMsMax: ftMax,
            SpriteDrawCallsAvg: clyTotal / samples,
            GlDrawCallsAvg: glTotal / samples,
            BatchCountAvg: batchTotal / samples,
            LargestBatchVerticesAvg: largestVTotal / samples)
        {
            SimpleSpriteCountAvg = samples > 0 ? (int)(simpleTotal / samples) : 0,
            FullSpriteCountAvg = samples > 0 ? (int)(fullTotal / samples) : 0,
        };
    }
}

/// <summary>
///     Lightweight DTO describing one entity the user can place from the
///     editor palette. Kept minimal so it crosses the WPF/RT thread
///     boundary without any RT types attached.
/// </summary>
public sealed record SpawnablePrototype(string Id, string Name);
