using System.Linq;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereWeatherDeviceSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<WeatherDeviceComponent, List<TileAtmosphere>> _cache = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<WeatherDeviceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WeatherDeviceComponent, ComponentShutdown>(OnShutdown);

        base.Initialize();
    }

    private void OnStartup(Entity<WeatherDeviceComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.EnabledChangeTime = _time.CurTime;
    }

    private void OnShutdown(Entity<WeatherDeviceComponent> ent, ref ComponentShutdown args)
    {
        _cache.Remove(ent.Comp);
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<WeatherDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var device, out var transform))
        {
            if (!device.Enabled)
            {
                continue;
            }

            device.StateMachine ??= GetStateMachine(device);
            
            var diff = _time.CurTime - device.LastChanged;
            if (diff < (device.StateMachine.Current.TickSpanCasted ?? device.DefaultTickSpanCasted))
            {
                return;
            }

            device.LastChanged = _time.CurTime;

            var map = transform.MapID;

            if (device.StateMachine.TrySwitchState(_time, _time.CurTime, device.EnabledChangeTime))
            {
                var targetWeather = device.StateMachine.Current.SetWeatherTo;
                if (targetWeather != null)
                {
                    var endTime = _time.CurTime + device.StateMachine.Current.WeatherOff + TimeSpan.FromSeconds(5);
                    var weather = _prototypeManager.Index(targetWeather.Value);

                    _weather.SetWeather(map, weather, endTime);
                }
            }

            if (!transform.GridUid.HasValue)
            {
                continue;
            }

            var gridUid = transform.GridUid.Value;

            EnsureComp<MapAtmosphereComponent>(transform.MapUid!.Value, out var mapAtmosphere);

            var temperatureChange = device.StateMachine.Current.TickRate;
            var targetTemperature = device.StateMachine.NextTargetTemp ?? device.StateMachine.Current.TargetTemperature;
            if (temperatureChange != 0)
            {
                if (!_cache.TryGetValue(device, out var list))
                {
                    list = new List<TileAtmosphere>();

                    EnsureComp<MapGridComponent>(gridUid, out var grid);
                    EnsureComp<GridAtmosphereComponent>(gridUid, out var gridAtmosphereComponent);

                    foreach (var (_, tile) in gridAtmosphereComponent.Tiles)
                    {
                        var vector = tile.GridIndices;
                        var entityCoordinates = new EntityCoordinates(gridUid, vector);
                        var refTile = _map.GetTileRef((gridUid, grid), entityCoordinates);
                        var tileDef = (ContentTileDefinition)_tileDefinitionManager[refTile.Tile.TypeId];
                        if (refTile.Tile.IsEmpty || !tileDef.CanCrowbar && tile.Air is { Immutable: false })
                        {
                            list.Add(tile);
                        }
                    }

                    _cache.Add(device, list);
                }

                Log.Debug("Changing temp for {count} tiles! {change}",list.Count, temperatureChange.ToString("F1"));
                bool isCooling = temperatureChange < 0;
                foreach (var tileAtmosphere in list)
                {
                    var mixture = tileAtmosphere.Air!;
                    if (tileAtmosphere.Air == null)
                    {
                        continue;
                    }

                    if (
                        targetTemperature != 0
                        && (
                            isCooling && mixture.Temperature >= targetTemperature
                            || !isCooling && mixture.Temperature <= targetTemperature
                            )
                        )
                    {
                        mixture.Temperature += temperatureChange;
                    }
                }

                var currentGas = mapAtmosphere.Mixture;

                if (
                    targetTemperature != 0
                    && (
                        isCooling && currentGas.Temperature >= targetTemperature
                        || !isCooling && currentGas.Temperature <= targetTemperature
                    )
                    )
                {
                    if (currentGas.Immutable)
                    {
                        var newGas = new GasMixture();
                        newGas.CopyFrom(currentGas);
                        currentGas = newGas;
                    }

                    currentGas.Temperature += temperatureChange;
                    _atmosphere.SetMapAtmosphere(transform.MapUid!.Value, false, currentGas);
                }
            }
        }
    }

    private WeatherStateMachine GetStateMachine(WeatherDeviceComponent device)
    {
        var deviceDefaultTickSpan = TimeSpan.Parse(device.DefaultTickSpan);
        device.DefaultTickSpanCasted = deviceDefaultTickSpan;

        var byKeyFrame = device.KeyFramesByCycleState.ToDictionary(
            x => TimeSpan.Parse(x.Key),
            x => x.Value
        );

        var byKeyFrameSequence = byKeyFrame.Select(x => (Span:x.Key,State: x.Value)).ToArray();

        (TimeSpan Span, WeatherCycleState State) lastWeatherChange = default;
        for (int i = 0; i < byKeyFrameSequence.Length; i++)
        {
            var (currentSpan, currentState)= byKeyFrameSequence[i];

            if (i + 1 < byKeyFrameSequence.Length)
            {
                var (nextSpan, nextState) = byKeyFrameSequence[i + 1];
                if (currentState.TargetTemperature.HasValue && nextState.TargetTemperature.HasValue)
                {
                    if (currentState.TickSpan != null)
                    {
                        currentState.TickSpanCasted = TimeSpan.Parse(currentState.TickSpan);
                    }

                    var tickSpan = currentState.TickSpanCasted ?? deviceDefaultTickSpan;
                    var tickCount = (nextSpan - currentSpan) / tickSpan;

                    currentState.TickRate = (float)((nextState.TargetTemperature.Value - currentState.TargetTemperature.Value) / tickCount);
                }
            }

            if (currentState.ResetWeather)
            {
                lastWeatherChange.State!.WeatherOff = currentSpan - lastWeatherChange.Span;
            }

            if (!currentState.SetWeatherTo.HasValue)
            {
                continue;
            }

            if (lastWeatherChange == default)
            {
                lastWeatherChange = (currentSpan, currentState);
                continue;
            }

            lastWeatherChange.State!.WeatherOff = currentSpan - lastWeatherChange.Span;
            lastWeatherChange = (currentSpan, currentState);
        }

        return new WeatherStateMachine(byKeyFrame, Log);
    }
}

public class WeatherStateMachine
{
    private readonly LinkedList<(TimeSpan Span, WeatherCycleState State)> _nodes;
    private readonly ISawmill _sawmill;
    private LinkedListNode<(TimeSpan Span, WeatherCycleState State)> _current;
    private readonly long _fullCycleTime;
    private readonly LinkedListNode<(TimeSpan Span, WeatherCycleState State)> _last;

    public WeatherStateMachine(Dictionary<TimeSpan, WeatherCycleState> nodes, ISawmill sawmill)
    {
        _nodes = new LinkedList<(TimeSpan Span, WeatherCycleState State)>(nodes.Select(x=>(x.Key,x.Value)));
        _current = _nodes.First!;
        _last = _nodes.Last!;
        _sawmill = sawmill;
        _fullCycleTime = (long)nodes.Keys.Max().TotalSeconds;
    }

    public bool TrySwitchState(
        IGameTiming timing,
        TimeSpan currentTimeInCycle,
        TimeSpan deviceEnableChangedTime
    )
    {
        var timeSinceEnabled = currentTimeInCycle - deviceEnableChangedTime;
        var currentSpan = TimeSpan.FromSeconds((long)timeSinceEnabled.TotalSeconds % _fullCycleTime);

        if (currentSpan < _current.Value.Span)
        {
            _current = _nodes.First!;
            _sawmill.Debug(
                "[{time}] Changed state! {partOfCycle}, {tempDiff}, {targetTemp} {weather}, {off}",
                timing.CurTime.ToString(@"hh\:mm\:ss"),
                _current.Value.Span.ToString(@"hh\:mm\:ss"),
                _current.Value.State.SetWeatherTo?.Id,
                _current.Value.State.TickRate.ToString("F1"),
                _current.Value.State.TargetTemperature?.ToString("F1"),
                _current.Value.State.WeatherOff?.ToString(@"hh\:mm\:ss")
            );
            return true;
        }

        if (currentSpan > _current.Value.Span && currentSpan < _current.Next!.Value.Span)
        {
            return false;
        }

        var next = _current.Next;
        do
        {
            if (next != null && currentSpan >= next.Value.Span)
            {
                _current = next;
                _sawmill.Debug(
                    "[{time}] Changed state! {partOfCycle}, {tempDiff}, {targetTemp} {weather}, {off}",
                    timing.CurTime.ToString(@"hh\:mm\:ss"),
                    _current.Value.Span.ToString(@"hh\:mm\:ss"),
                    _current.Value.State.SetWeatherTo?.Id,
                    _current.Value.State.TickRate.ToString("F1"),
                    _current.Value.State.TargetTemperature?.ToString("F1"),
                    _current.Value.State.WeatherOff?.ToString(@"hh\:mm\:ss")
                );
                return true;
            }

            next = next!.Next;
        } while (_current.Next != null && currentSpan > _current.Next.Value.Span);

        return false;
    }



    public WeatherCycleState Current { get => _current.Value.State; }
    public float? NextTargetTemp => _current.Next?.Value.State.TargetTemperature;
}
