using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Stealth.Components;
using Content.Shared.Whitelist;
using Content.Shared.Overlays;
using Content.Shared.Inventory;
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
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    [Dependency] private readonly InventorySystem _inventorySystem = default!;

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
    public bool IsVisible(Entity<MetaDataComponent> ent, StatusIconData data)
    {
        var viewer = _playerManager.LocalSession?.AttachedEntity;

        // Always show our icons to our entity
        if (viewer == ent.Owner)
            return true;

        if (data.VisibleToGhosts && HasComp<GhostComponent>(viewer))
            return true;

        if (data.HideInContainer && (ent.Comp.Flags & MetaDataFlags.InContainer) != 0)
            return false;

        if (data.HideOnStealth && TryComp<StealthComponent>(ent, out var stealth) && stealth.Enabled)
            return false;

        if (data.ShowTo != null && !_entityWhitelist.IsValid(data.ShowTo, viewer))
            return false;

        return true;
    }

    /// <summary>
    /// For overlay to check if an entity has the ShowHealthIconsComponent attached to it or any of its items
    /// and returns true if the icon should be visible.
    /// </summary>
    public bool ShouldHideIcon(EntityUid uid)
    {
        //  first check if the entity itself has the component
        if(TryComp<ShowHealthIconsComponent>(uid, out var component) && component != null)
        {
            return !component.IsVisible;
        }

        // then check every slot if any of the items have the component
        if (TryComp<InventoryComponent>(uid, out var inventoryComponent))
        {
            foreach (var slot in inventoryComponent.Slots)
            {
                if (slot == null)
                    continue;

                if (_inventorySystem.TryGetSlotEntity(uid, slot.Name, out var slotEntity))
                {
                    if(TryComp<ShowHealthIconsComponent>(slotEntity, out var slotComponent) && slotComponent != null)
                    {
                        return !slotComponent.IsVisible;
                    }
                }
            }
        }

        return false;
    }
}
