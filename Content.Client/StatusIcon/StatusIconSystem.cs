using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client.StatusIcon;

/// <summary>
/// This handles rendering gathering and rendering icons on entities.
/// </summary>
public sealed class StatusIconSystem : SharedStatusIconSystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

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

        var ev = new GetStatusIconsEvent(list);
        RaiseLocalEvent(uid, ref ev);
        return ev.StatusIcons;
    }

    /// <summary>
    /// For overlay to check if an entity can be seen.
    /// </summary>
    public bool IsVisible(Entity<MetaDataComponent> ent, StatusIconData proto)
    {
        // ghosties can always see them
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (HasComp<GhostComponent>(viewer))
            return true;

        if (proto.HideInContainer && (ent.Comp.Flags & MetaDataFlags.InContainer) != 0)
            return false;

        if (proto.HideTo.IsValid(ent, EntityManager))
            return false;

        if (!proto.ShowTo.IsValid(ent, EntityManager))
            return false;

        return true;
    }
}
