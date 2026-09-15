using Content.Client.Atmos.Overlays;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
///     System responsible for rendering vacuum effects using <see cref="GasTileVacuumOverlay"/>.
/// </summary>
[UsedImplicitly]
public sealed partial class GasTileVacuumOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private GasTileVacuumOverlay _gasTileVacuumOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _gasTileVacuumOverlay = new GasTileVacuumOverlay();
        _overlayMan.AddOverlay(_gasTileVacuumOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<GasTileVacuumOverlay>();
    }
}
