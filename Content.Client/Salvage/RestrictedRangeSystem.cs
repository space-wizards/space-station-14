using Content.Client.Salvage.UI;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Salvage;

public sealed class RestrictedRangeSystem : SharedRestrictedRangeSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RestrictedRangeOverlay(_clyde, EntityManager, _mapManager, _protoManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<RestrictedRangeOverlay>();
    }
}
