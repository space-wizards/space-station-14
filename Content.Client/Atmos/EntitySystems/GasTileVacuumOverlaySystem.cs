using Content.Client.Atmos.Overlays;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
///     System responsible for rendering vacuum effects using <see cref="GasTileVacuumOverlay"/>.
/// </summary>
[UsedImplicitly]
public sealed partial class GasTileVacuumOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;

    private GasTileVacuumOverlay? _gasTileVacuumOverlay;

    public override void Initialize()
    {
        base.Initialize();
        _cfgManager.OnValueChanged(CCVars.VacuumOverlay, OnVacuumOverlayChanged, true);
        _cfgManager.OnValueChanged(CCVars.VacuumOverlayIntensity, OnVacuumOverlayIntensityChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfgManager.UnsubValueChanged(CCVars.VacuumOverlay, OnVacuumOverlayChanged);
        _overlayMan.RemoveOverlay<GasTileVacuumOverlay>();
    }

    private void OnVacuumOverlayChanged(bool enabled)
    {
        if (enabled)
        {
            _gasTileVacuumOverlay ??= new GasTileVacuumOverlay();

            if (!_overlayMan.HasOverlay<GasTileVacuumOverlay>())
                _overlayMan.AddOverlay(_gasTileVacuumOverlay);
        }
        else
        {
            _overlayMan.RemoveOverlay<GasTileVacuumOverlay>();
        }
    }

    private void OnVacuumOverlayIntensityChanged(float intensity)
    {
        if (intensity > 0f)
        {
            _gasTileVacuumOverlay ??= new GasTileVacuumOverlay();

            if (!_overlayMan.HasOverlay<GasTileVacuumOverlay>())
                _overlayMan.AddOverlay(_gasTileVacuumOverlay);
        }
        else
        {
            _overlayMan.RemoveOverlay<GasTileVacuumOverlay>();
        }
    }
}
