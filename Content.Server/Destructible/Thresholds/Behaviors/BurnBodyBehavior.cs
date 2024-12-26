using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class BurnBodyBehavior : IThresholdBehavior
{

    public void Execute(EntityUid bodyId, DestructibleSystem system, EntityUid? cause = null)
    {
        var transformSystem = system.EntityManager.System<TransformSystem>();
        var inventorySystem = system.EntityManager.System<InventorySystem>();
        var sharedPopupSystem = system.EntityManager.System<SharedPopupSystem>();

        if (system.EntityManager.TryGetComponent<InventoryComponent>(bodyId, out var comp))
        {
            foreach (var item in inventorySystem.GetHandOrInventoryEntities(bodyId))
            {
                transformSystem.DropNextTo(item, bodyId);
            }
        }

        sharedPopupSystem.PopupCoordinates(Loc.GetString("bodyburn-text-others", ("name", bodyId)), transformSystem.GetMoverCoordinates(bodyId), PopupType.LargeCaution);

        system.EntityManager.QueueDeleteEntity(bodyId);
    }
}
