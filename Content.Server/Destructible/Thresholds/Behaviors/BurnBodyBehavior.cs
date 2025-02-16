using Content.Shared.Body.Components;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class BurnBodyBehavior : IThresholdBehavior
{

    public void Execute(EntityUid bodyId,
        IDependencyCollection collection,
        EntityManager entManager,
        EntityUid? cause = null)
    {
        var transformSystem = entManager.System<TransformSystem>();
        var inventorySystem = entManager.System<InventorySystem>();
        var sharedPopupSystem = entManager.System<SharedPopupSystem>();

        if (entManager.TryGetComponent<InventoryComponent>(bodyId, out var comp))
        {
            foreach (var item in inventorySystem.GetHandOrInventoryEntities(bodyId))
            {
                transformSystem.DropNextTo(item, bodyId);
            }
        }

        sharedPopupSystem.PopupCoordinates(Loc.GetString("bodyburn-text-others", ("name", bodyId)), transformSystem.GetMoverCoordinates(bodyId), PopupType.LargeCaution);

        entManager.QueueDeleteEntity(bodyId);
    }
}
