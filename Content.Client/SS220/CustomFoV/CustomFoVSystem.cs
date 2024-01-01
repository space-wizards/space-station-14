// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CustomFoV;

public sealed class CustomFoVSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private CustomFoVOverlay? _fovOverlay;

    public override void Initialize()
    {
        base.Initialize();

        _fovOverlay = new(EntityManager, _prototype);
        _overlay.AddOverlay(_fovOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_fovOverlay is not null)
            _overlay.RemoveOverlay(_fovOverlay);
    }
}
