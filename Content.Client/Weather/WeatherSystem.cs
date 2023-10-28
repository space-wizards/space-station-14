using System.Numerics;
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
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

    protected override void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime)
    {
        base.Run(uid, weather, weatherProto, frameTime);

        var ent = _playerManager.LocalPlayer?.ControlledEntity;

        if (ent == null)
            return;

        var mapUid = Transform(uid).MapUid;
        var entXform = Transform(ent.Value);

        // Maybe have the viewports manage this?
        if (mapUid == null || entXform.MapUid != mapUid)
        {
            weather.LastOcclusion = 0f;
            weather.LastAlpha = 0f;
            weather.Stream?.Stop();
            weather.Stream = null;
            return;
        }

        if (!Timing.IsFirstTimePredicted || weatherProto.Sound == null)
            return;

        weather.Stream ??= _audio.PlayGlobal(weatherProto.Sound, Filter.Local(), true);
        var volumeMod = MathF.Pow(10, weatherProto.Sound.Params.Volume / 10f);

        var stream = (AudioSystem.PlayingStream) weather.Stream!;
        var alpha = weather.LastAlpha;
        alpha = MathF.Pow(alpha, 2f) * volumeMod;
        // TODO: Lerp this occlusion.
        var occlusion = 0f;
        // TODO: Fade-out needs to be slower
        // TODO: HELPER PLZ

        // Work out tiles nearby to determine volume.
        if (TryComp<MapGridComponent>(entXform.GridUid, out var grid))
        {
            var gridId = entXform.GridUid.Value;
            // Floodfill to the nearest tile and use that for audio.
            var seed = _mapSystem.GetTileRef(gridId, grid, entXform.Coordinates);
            var frontier = new Queue<TileRef>();
            frontier.Enqueue(seed);
            // If we don't have a nearest node don't play any sound.
            EntityCoordinates? nearestNode = null;
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var weatherIgnoreQuery = GetEntityQuery<IgnoreWeatherComponent>();
            var visited = new HashSet<Vector2i>();

            while (frontier.TryDequeue(out var node))
            {
                if (!visited.Add(node.GridIndices))
                    continue;

                if (!CanWeatherAffect(grid, node, weatherIgnoreQuery, bodyQuery))
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
                                (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length() > 3)
                            {
                                continue;
                            }

                            frontier.Enqueue(_mapSystem.GetTileRef(gridId, grid, new Vector2i(x, y) + node.GridIndices));
                        }
                    }

                    continue;
                }

                nearestNode = new EntityCoordinates(entXform.GridUid.Value,
                    (Vector2) node.GridIndices + (grid.TileSizeHalfVector));
                break;
            }

            if (nearestNode == null)
                alpha = 0f;
            else
            {
                var entPos = _transform.GetWorldPosition(entXform);
                var sourceRelative = nearestNode.Value.ToMap(EntityManager).Position - entPos;

                if (sourceRelative.LengthSquared() > 1f)
                {
                    occlusion = _physics.IntersectRayPenetration(entXform.MapID,
                        new CollisionRay(entPos, sourceRelative.Normalized(), _audio.OcclusionCollisionMask),
                        sourceRelative.Length(), stream.TrackingEntity);
                }
            }
        }

        if (MathHelper.CloseTo(weather.LastOcclusion, occlusion, 0.01f))
            weather.LastOcclusion = occlusion;
        else
            weather.LastOcclusion += (occlusion - weather.LastOcclusion) * OcclusionLerpRate * frameTime;

        if (MathHelper.CloseTo(weather.LastAlpha, alpha, 0.01f))
            weather.LastAlpha = alpha;
        else
            weather.LastAlpha += (alpha - weather.LastAlpha) * AlphaLerpRate * frameTime;

        // Full volume if not on grid
        stream.Source.SetVolumeDirect(weather.LastAlpha);
        stream.Source.SetOcclusion(weather.LastOcclusion);
    }

    protected override void EndWeather(EntityUid uid, WeatherComponent component, string proto)
    {
        base.EndWeather(uid, component, proto);

        if (!component.Weather.TryGetValue(proto, out var weather))
            return;

        weather.LastAlpha = 0f;
        weather.LastOcclusion = 0f;
    }

    protected override bool SetState(WeatherState state, WeatherComponent comp, WeatherData weather, WeatherPrototype weatherProto)
    {
        if (!base.SetState(state, comp, weather, weatherProto))
            return false;

        if (!Timing.IsFirstTimePredicted)
            return true;

        // TODO: Fades (properly)
        weather.Stream?.Stop();
        weather.Stream = null;
        weather.Stream = _audio.PlayGlobal(weatherProto.Sound, Filter.Local(), true);
        return true;
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        foreach (var (proto, weather) in component.Weather)
        {
            // End existing one
            if (!state.Weather.TryGetValue(proto, out var stateData))
            {
                EndWeather(uid, component, proto);
                continue;
            }

            // Data update?
            weather.StartTime = stateData.StartTime;
            weather.EndTime = stateData.EndTime;
            weather.State = stateData.State;
        }

        foreach (var (proto, weather) in state.Weather)
        {
            if (component.Weather.ContainsKey(proto))
                continue;

            // New weather
            StartWeather(component, ProtoMan.Index<WeatherPrototype>(proto), weather.EndTime);
            weather.LastAlpha = 0f;
        }
    }
}
