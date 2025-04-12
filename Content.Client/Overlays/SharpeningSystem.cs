using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Client.Graphics;
using Content.Shared.CCVar;
using Content.Client.Overlays;

namespace Content.Client.Overlays;

public sealed class SharpeningSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private SharpeningOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new SharpeningOverlay();
        _overlayManager.AddOverlay(_overlay);

        var initialSharpness = _cfg.GetCVar(CCVars.DisplaySharpening);
        OnSharpnessChanged(initialSharpness);

        _cfg.OnValueChanged(CCVars.DisplaySharpening, OnSharpnessChanged);
    }

    private void OnSharpnessChanged(int value)
    {
        _overlay.Sharpness = value / 10f;
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay(_overlay);
        base.Shutdown();
    }

    public void SetSharpness(float value)
    {
        _overlay.Sharpness = value;
    }
}
