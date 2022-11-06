using Content.Shared.Weather;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.AddOverlay(new WeatherOverlay());
        SubscribeLocalEvent<WeatherComponent, ComponentHandleState>(OnWeatherHandleState);
    }

    protected override void Run(WeatherPrototype weather, WeatherState state)
    {
        base.Run(weather, state);

        var ent = _playerManager.LocalPlayer?.ControlledEntity;

        if (ent == null)
            return;

        var sound = weather.Sound;

        if (sound != null)
        {
            // TODO: Stream neowwwwwwwwww
        }

        // TODO: Frames and stuff
        // TODO: Audio
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        component.EndTime = state.EndTime;
        component.Weather = state.Weather;
        component.Duration = state.Duration;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.RemoveOverlay<WeatherOverlay>();
    }
}
