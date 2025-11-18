using Robust.Client.Graphics;

namespace Content.Client.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeEmergency();
        _overlays.AddOverlay(new FtlArrivalOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay<FtlArrivalOverlay>();
    }
}
