using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Content.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Overlays;

public sealed class DesertMirageOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IEyeManager _eyeMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private DesertMirageOverlay? _overlay;
    private bool _overlayAdded;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new DesertMirageOverlay();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlayMan.RemoveOverlay(_overlay);
        _overlayAdded = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_overlay == null)
            return;

        var mapId = _eyeMan.CurrentMap;
        if (mapId == null)
        {
            EnsureOverlay(false);
            return;
        }

        DesertMirageMapComponent? chosen = null;
        var query = _entMan.EntityQueryEnumerator<DesertMirageMapComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.MapID == mapId)
            {
                if (comp.Enabled)
                {
                    chosen = comp;
                    break;
                }
            }
        }

        if (chosen != null)
        {
            if (chosen.Strength is { } strength)
                _overlay.Strength = strength;
            if (chosen.Speed is { } speed)
                _overlay.Speed = speed;
            if (chosen.DistortScale is { } ds)
                _overlay.DistortScale = ds;
            if (chosen.VerticalBias is { } vb)
                _overlay.VerticalBias = vb;

            EnsureOverlay(true);
        }
        else
        {
            EnsureOverlay(false);
        }
    }

    private void EnsureOverlay(bool shouldBeOn)
    {
        if (_overlay == null)
            return;

        if (shouldBeOn && !_overlayAdded)
        {
            _overlayMan.AddOverlay(_overlay);
            _overlayAdded = true;
        }
        else if (!shouldBeOn && _overlayAdded)
        {
            _overlayMan.RemoveOverlay(_overlay);
            _overlayAdded = false;
        }
    }
}
