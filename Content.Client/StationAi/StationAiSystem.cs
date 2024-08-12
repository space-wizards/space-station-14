using System.Numerics;
using Content.Client.SurveillanceCamera;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;

namespace Content.Client.StationAi;

public sealed class StationAiSystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IOverlayManager _overlayMgr = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private StationAiOverlay? _overlay;

    public bool Enabled => _overlay != null;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new StationAiOverlay();
        _overlayMgr.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMgr.RemoveOverlay<StationAiOverlay>();
    }
}
