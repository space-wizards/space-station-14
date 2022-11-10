using Content.Shared.Weather;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
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

        if (mapUid == null)
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
            // TODO: HELPER PLZ

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
