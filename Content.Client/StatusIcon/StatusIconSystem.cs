using Content.Shared.CCVar;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.StatusIcon;

/// <summary>
/// This handles rendering gathering and rendering icons on entities.
/// </summary>
public sealed class StatusIconSystem : SharedStatusIconSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private bool _globalEnabled;
    private bool _localEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        Subs.CVar(_configuration, CCVars.LocalStatusIconsEnabled, OnLocalStatusIconChanged, true);
        Subs.CVar(_configuration, CCVars.GlobalStatusIconsEnabled, OnGlobalStatusIconChanged, true);
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
        if (_overlay.RemoveOverlay<StatusIconOverlay>())
            return;

        if (_globalEnabled && _localEnabled)
            _overlay.AddOverlay(new StatusIconOverlay());
    }

    public List<StatusIconData> GetStatusIcons(EntityUid uid, MetaDataComponent? meta = null)
    {
        var list = new List<StatusIconData>();
        if (!Resolve(uid, ref meta))
            return list;

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
            return list;

        var inContainer = (meta.Flags & MetaDataFlags.InContainer) != 0;
        var ev = new GetStatusIconsEvent(list, inContainer);
        RaiseLocalEvent(uid, ref ev);
        return ev.StatusIcons;
    }
}

