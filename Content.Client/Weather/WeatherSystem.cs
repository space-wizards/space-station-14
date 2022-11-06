using Content.Shared.Weather;
using Robust.Client.Graphics;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    public override void Initialize()
    {
        base.Initialize();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.AddOverlay(new WeatherOverlay());
        SubscribeLocalEvent<WeatherComponent;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.RemoveOverlay<WeatherOverlay>();
    }
}
