using Robust.Client.Graphics;

namespace Content.Client.Placement;

/// <summary>
/// Manages the <see cref="PlacementDirectionIndicatorOverlay"/>.
/// </summary>
public sealed partial class PlacementDirectionIndicatorSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new PlacementDirectionIndicatorOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<PlacementDirectionIndicatorOverlay>();
    }
}
