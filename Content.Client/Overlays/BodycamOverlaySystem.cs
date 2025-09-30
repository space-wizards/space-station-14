using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Content.Client.Administration.Managers;

namespace Content.Client.Overlays;

public sealed class BodycamOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    private BodycamOverlay? _post;
    private BodycamHudOverlay? _hud;

    public override void Initialize()
    {
        base.Initialize();

        _post = new BodycamOverlay();
        _hud = new BodycamHudOverlay();

        // React to CVar changes
        _cfg.OnValueChanged(CCVars.HudBodycamEnabled, OnEnabledChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCVars.HudBodycamEnabled, OnEnabledChanged);
        SetEnabled(false);
    }

    private void OnEnabledChanged(bool enabled)
    {
        // Only admins are allowed to disable bodycam.
        if (!enabled && !_admin.IsAdmin())
        {
            // Force it back on for non-admins.
            if (!_cfg.GetCVar(CCVars.HudBodycamEnabled))
                _cfg.SetCVar(CCVars.HudBodycamEnabled, true);
            return;
        }

        SetEnabled(enabled);
    }

    private void SetEnabled(bool enabled)
    {
        if (_post == null || _hud == null)
            return;

        var hasPost = _overlayMan.HasOverlay<BodycamOverlay>();
        var hasHud = _overlayMan.HasOverlay<BodycamHudOverlay>();

        if (enabled)
        {
            if (!hasPost)
                _overlayMan.AddOverlay(_post);
            if (!hasHud)
                _overlayMan.AddOverlay(_hud);
        }
        else
        {
            if (hasPost)
                _overlayMan.RemoveOverlay<BodycamOverlay>();
            if (hasHud)
                _overlayMan.RemoveOverlay<BodycamHudOverlay>();
        }
    }
}
