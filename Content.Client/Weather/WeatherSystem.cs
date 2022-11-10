using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.AddOverlay(new WeatherOverlay(EntityManager.System<SpriteSystem>(), this));
        SubscribeLocalEvent<WeatherComponent, ComponentHandleState>(OnWeatherHandleState);
    }

    protected override void Run(WeatherComponent component, WeatherPrototype weather, WeatherState state)
    {
        base.Run(component, weather, state);

        var ent = _playerManager.LocalPlayer?.ControlledEntity;

        if (ent == null)
            return;

        var mapUid = Transform(component.Owner).MapUid;
        var entXform = Transform(ent.Value);

        // Maybe have the viewports manage this?
        if (mapUid == null || entXform.MapUid != mapUid)
        {
            component.Stream?.Stop();
            component.Stream = null;
            return;
        }

        // TODO: Average alpha across nearby 2x2 tiles.
        // At least, if we can change position

        if (_timing.IsFirstTimePredicted && component.Stream != null)
        {
            var stream = (AudioSystem.PlayingStream) component.Stream;
            var alpha = GetPercent(component, mapUid.Value, weather);
            alpha = MathF.Pow(alpha, 4f);
            // TODO: Fade-out needs to be slower
            // TODO: HELPER PLZ
            var circle = new Circle(entXform.WorldPosition, 3f);

            // Work out tiles nearby to also determine volume.
            if (entXform.GridUid != null)
            {
                // If the tile count is less than target tiles we just assume the difference is weather-affected.
                var targetTiles = 25;
                var tiles = 0;
                var weatherTiles = 0;

                // TODO: Floodfill out and determine if we should occlude.
                // TODO: Floodfill distance to nearest tile determines volume, too.

                foreach (var tile in MapManager.GetGrid(entXform.GridUid.Value).GetTilesIntersecting(circle))
                {
                    tiles++;

                    if (weather.Tiles.Contains(_tileDefManager[tile.Tile.TypeId].ID))
                    {
                        weatherTiles++;
                    }
                }

                var totalWeather = Math.Max(0, weatherTiles + targetTiles - tiles);
                var ratio = Math.Clamp(tiles == 0 ? 1f : totalWeather / (float) tiles, 0f, 1f);
                alpha *= ratio;
            }
            // Full volume if not on grid

            stream.Gain = alpha;
        }

        // TODO: Frames and stuff
        // TODO: Audio
    }

    public float GetPercent(WeatherComponent component, EntityUid mapUid, WeatherPrototype weatherProto)
    {
        var pauseTime = _metadata.GetPauseTime(mapUid);
        var elapsed = _timing.CurTime - (component.StartTime + pauseTime);
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

    protected override bool SetState(WeatherComponent component, WeatherState state, WeatherPrototype prototype)
    {
        if (!base.SetState(component, state, prototype))
            return false;

        if (_timing.IsFirstTimePredicted)
        {
            // TODO: Fades
            component.Stream?.Stop();
            component.Stream = null;
            component.Stream = _audio.PlayGlobal(prototype.Sound, Filter.Local());
        }

        return true;
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        if (component.Weather != state.Weather || !component.EndTime.Equals(state.EndTime) || !component.StartTime.Equals(state.StartTime))
        {
            EndWeather(component);

            if (state.Weather != null)
                StartWeather(component, _protoMan.Index<WeatherPrototype>(state.Weather));
        }

        component.EndTime = state.EndTime;
        component.StartTime = state.StartTime;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.RemoveOverlay<WeatherOverlay>();
    }
}
