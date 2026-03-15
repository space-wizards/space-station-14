using Content.Client.Atmos.Overlays;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
///     System responsible for rendering visible atmos gasses (like plasma for example) using <see cref="GasTileVisibleGasOverlay"/>.
/// </summary>
[UsedImplicitly]
public sealed class GasTileVisibleGasOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private GasTileVisibleGasOverlay _visibleGasOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _visibleGasOverlay = new GasTileVisibleGasOverlay();
        _overlayMan.AddOverlay(_visibleGasOverlay);
        Subs.CVar(_cfg, CCVars.GasOverlaySmoothingSubdivisionsPerAxis, value => _visibleGasOverlay.SetSmoothingSubdivisionsPerAxis(value), true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<GasTileVisibleGasOverlay>();
    }

}
