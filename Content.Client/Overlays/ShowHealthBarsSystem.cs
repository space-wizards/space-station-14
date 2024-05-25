using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using System.Linq;
using Robust.Client.Player;

namespace Content.Client.Overlays;

/// <summary>
/// Adds a health bar overlay.
/// </summary>
public sealed class ShowHealthBarsSystem : EquipmentHudSystem<ShowHealthBarsComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private EntityHealthBarOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new(EntityManager, _playerManager);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthBarsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            _overlay.DamageContainers.Add(damageContainerId);
        }

        if (!_overlayMan.HasOverlay<EntityHealthBarOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.DamageContainers.Clear();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
