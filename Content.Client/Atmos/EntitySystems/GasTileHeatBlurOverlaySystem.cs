using Content.Client.Atmos.Overlays;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
///     System responsible for rendering heat distortion using <see cref="GasTileHeatBlurOverlay"/>.
/// </summary>
[UsedImplicitly]
public sealed class GasTileHeatBlurOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private GasTileHeatBlurOverlay _gasTileHeatBlurOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _gasTileHeatBlurOverlay = new GasTileHeatBlurOverlay();
        _overlayMan.AddOverlay(_gasTileHeatBlurOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<GasTileHeatBlurOverlay>();
    }
}
