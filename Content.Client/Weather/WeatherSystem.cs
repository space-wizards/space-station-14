using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Consistency isn't really important, just want to avoid sharp changes and there's no way to lerp on engine nicely atm.
    private float _lastAlpha;
    private float _lastOcclusion;

    private const float OcclusionLerpRate = 4f;
    private const float AlphaLerpRate = 4f;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new WeatherOverlay(_transform, EntityManager.System<SpriteSystem>(), this));
        SubscribeLocalEvent<WeatherComponent, ComponentHandleState>(OnWeatherHandleState);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<WeatherOverlay>();
    }

    protected override void Run(EntityUid uid, WeatherComponent component, WeatherPrototype weather, WeatherState state, float frameTime)
    {
        base.Run(uid, component, weather, state, frameTime);

        var ent = _playerManager.LocalPlayer?.ControlledEntity;

        if (ent == null)
            return;

        var mapUid = Transform(uid).MapUid;
        var entXform = Transform(ent.Value);

        // Maybe have the viewports manage this?
        if (mapUid == null || entXform.MapUid != mapUid)
        {
            _lastOcclusion = 0f;
            _lastAlpha = 0f;
            component.Stream?.Stop();
            component.Stream = null;
            return;
        }

        if (!Timing.IsFirstTimePredicted || weather.Sound == null)
            return;

        component.Stream ??= _audio.PlayGlobal(weather.Sound, Filter.Local(), true);
        var volumeMod = MathF.Pow(10, weather.Sound.Params.Volume / 10f);

        var stream = (AudioSystem.PlayingStream) component.Stream!;
        var alpha = GetPercent(component, mapUid.Value, weather);
        alpha = MathF.Pow(alpha, 2f) * volumeMod;
        // TODO: Lerp this occlusion.
        var occlusion = 0f;
        // TODO: Fade-out needs to be slower
        // TODO: HELPER PLZ

        // Work out tiles nearby to determine volume.
        if (TryComp<MapGridComponent>(entXform.GridUid, out var grid))
        {
            // Floodfill to the nearest tile and use that for audio.
            var seed = grid.GetTileRef(entXform.Coordinates);
            var frontier = new Queue<TileRef>();
            frontier.Enqueue(seed);
            // If we don't have a nearest node don't play any sound.
            EntityCoordinates? nearestNode = null;
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var visited = new HashSet<Vector2i>();

            while (frontier.TryDequeue(out var node))
            {
                if (!visited.Add(node.GridIndices))
                    continue;

                if (!CanWeatherAffect(grid, node, bodyQuery))
                {
                    // Add neighbors
                    // TODO: Ideally we pick some deterministically random direction and use that
                    // We can't just do that naively here because it will flicker between nearby tiles.
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (Math.Abs(x) == 1 && Math.Abs(y) == 1 ||
                                x == 0 && y == 0 ||
                                (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length > 3)
                            {
                                continue;
                            }

                            frontier.Enqueue(grid.GetTileRef(new Vector2i(x, y) + node.GridIndices));
                        }
                    }

                    continue;
                }

                nearestNode = new EntityCoordinates(entXform.GridUid.Value,
                    (Vector2) node.GridIndices + (grid.TileSize / 2f));
                break;
            }

            if (nearestNode == null)
                alpha = 0f;
            else
            {
                var entPos = _transform.GetWorldPosition(entXform);
                var sourceRelative = nearestNode.Value.ToMap(EntityManager).Position - entPos;

                if (sourceRelative.LengthSquared > 1f)
                {
                    occlusion = _physics.IntersectRayPenetration(entXform.MapID,
                        new CollisionRay(entPos, sourceRelative.Normalized, _audio.OcclusionCollisionMask),
                        sourceRelative.Length, stream.TrackingEntity);
                }
            }
        }

        if (MathHelper.CloseTo(_lastOcclusion, occlusion, 0.01f))
            _lastOcclusion = occlusion;
        else
            _lastOcclusion += (occlusion - _lastOcclusion) * OcclusionLerpRate * frameTime;

        if (MathHelper.CloseTo(_lastAlpha, alpha, 0.01f))
            _lastAlpha = alpha;
        else
            _lastAlpha += (alpha - _lastAlpha) * AlphaLerpRate * frameTime;

        // Full volume if not on grid
        stream.Source.SetVolumeDirect(_lastAlpha);
        stream.Source.SetOcclusion(_lastOcclusion);
    }

    public float GetPercent(WeatherComponent component, EntityUid mapUid, WeatherPrototype weatherProto)
    {
        var pauseTime = _metadata.GetPauseTime(mapUid);
        var elapsed = Timing.CurTime - (component.StartTime + pauseTime);
        var duration = component.Duration;
        var remaining = duration - elapsed;
        float alpha;

        if (elapsed < weatherProto.StartupTime)
        {
            alpha = (float) (elapsed / weatherProto.StartupTime);
        }
        else if (remaining < weatherProto.ShutdownTime)
        {
            alpha = (float) (remaining / weatherProto.ShutdownTime);
        }
        else
        {
            alpha = 1f;
        }

        return alpha;
    }

    protected override bool SetState(EntityUid uid, WeatherComponent component, WeatherState state, WeatherPrototype prototype)
    {
        if (!base.SetState(uid, component, state, prototype))
            return false;

        if (!Timing.IsFirstTimePredicted)
            return true;

        // TODO: Fades
        component.Stream?.Stop();
        component.Stream = null;
        component.Stream = _audio.PlayGlobal(prototype.Sound, Filter.Local(), true);
        return true;
    }

    protected override void EndWeather(WeatherComponent component)
    {
        _lastOcclusion = 0f;
        _lastAlpha = 0f;
        base.EndWeather(component);
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        if (component.Weather != state.Weather || !component.EndTime.Equals(state.EndTime) || !component.StartTime.Equals(state.StartTime))
        {
            EndWeather(component);

            if (state.Weather != null)
                StartWeather(component, ProtoMan.Index<WeatherPrototype>(state.Weather));
        }

        component.EndTime = state.EndTime;
        component.StartTime = state.StartTime;
    }
}
