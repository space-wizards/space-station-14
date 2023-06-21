using Content.Shared.CCVar;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.StatusIcon;

/// <summary>
/// This handles rendering gathering and rendering icons on entities.
/// </summary>
public sealed class StatusIconSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private bool _globalEnabled;
    private bool _localEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _configuration.OnValueChanged(CCVars.LocalStatusIconsEnabled, OnLocalStatusIconChanged, true);
        _configuration.OnValueChanged(CCVars.GlobalStatusIconsEnabled, OnGlobalStatusIconChanged, true);
    }
    private void OnLocalStatusIconChanged(bool obj)
    {
        _localEnabled = obj;
        UpdateOverlayVisible();
    }

    private void OnGlobalStatusIconChanged(bool obj)
    {
        _globalEnabled = obj;
        UpdateOverlayVisible();
    }

    private void UpdateOverlayVisible()
    {
        if (_globalEnabled && _localEnabled)
        {
            if (!_overlay.HasOverlay<StatusIconOverlay>())
                _overlay.AddOverlay(new StatusIconOverlay());
        }
        else
        {
            if (_overlay.HasOverlay<StatusIconOverlay>())
                _overlay.RemoveOverlay<StatusIconOverlay>();
        }
    }

    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public List<StatusIconData> GetStatusIcons(EntityUid uid)
    {
        if (!Exists(uid) || Terminating(uid))
            return new();

        var ev = new GetStatusIconsEvent(new());
        RaiseLocalEvent(uid, ref ev);

        ev.StatusIcons.Add(_prototype.Index<StatusIconPrototype>("DebugStatus2"));
        return ev.StatusIcons;
    }
}

