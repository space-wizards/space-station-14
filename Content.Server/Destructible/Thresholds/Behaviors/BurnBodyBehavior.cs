using JetBrains.Annotations;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class BurnBodyBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <summary>
    ///     The popup displayed upon destruction.
    /// </summary>
    [DataField]
    public LocId PopupMessage = "bodyburn-text-others";

    public void Execute(EntityUid bodyId, EntityUid? cause = null)
    {
        if (HasComp<InventoryComponent>(bodyId))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
            {
                _transform.DropNextTo(item, bodyId);
            }
        }

        var bodyIdentity = Identity.Entity(bodyId, EntityManager);
        _popup.PopupCoordinates(Loc.GetString(PopupMessage, ("name", bodyIdentity)), _transform.GetMoverCoordinates(bodyId), PopupType.LargeCaution);

        QueueDel(bodyId);
    }
}
