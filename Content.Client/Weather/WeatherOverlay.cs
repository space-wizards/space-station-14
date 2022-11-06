using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Weather;

public sealed class WeatherOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    // TODO: WeatherComponent on the map.
    // TODO: Fade-in
    // TODO: Scrolling(?) like parallax
    // TODO: Need affected tiles and effects to apply.

    protected override void Draw(in OverlayDrawArgs args)
    {
        throw new NotImplementedException();
    }
}
