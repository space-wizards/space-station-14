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

    private GasTileVacuumOverlay _gasTileVacuumOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        if (!_cfgManager.GetCVar(CCVars.VacuumOverlay))
            return;

        _gasTileVacuumOverlay = new GasTileVacuumOverlay();
        _overlayMan.AddOverlay(_gasTileVacuumOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<GasTileVacuumOverlay>();
    }
}
