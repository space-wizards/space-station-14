using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
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
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    private bool _globalEnabled;
    private bool _localEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        _configuration.OnValueChanged(CCVars.LocalStatusIconsEnabled, OnLocalStatusIconChanged, true);
        _configuration.OnValueChanged(CCVars.GlobalStatusIconsEnabled, OnGlobalStatusIconChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _configuration.UnsubValueChanged(CCVars.LocalStatusIconsEnabled, OnLocalStatusIconChanged);
        _configuration.UnsubValueChanged(CCVars.GlobalStatusIconsEnabled, OnGlobalStatusIconChanged);
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

    /// <summary>
    /// For overlay to check if an entity can be seen.
    /// </summary>
    public bool IsVisible(EntityUid uid)
    {
        // ghosties can always see them
        var viewer = _playerMan.LocalPlayer?.ControlledEntity;
        if (_ghostQuery.HasComponent(viewer))
            return true;

        if (_spriteQuery.TryGetComponent(uid, out var sprite) && !sprite.Visible)
            return false;

        var ev = new StatusIconVisibleEvent(true);
        RaiseLocalEvent(uid, ref ev);
        return ev.Visible;
    }
}

/// <summary>
/// Raised on an entity to check if it should draw hud icons.
/// Used to check invisibility etc inside the screen bounds.
/// </summary>
[ByRefEvent]
public record struct StatusIconVisibleEvent(bool Visible);
