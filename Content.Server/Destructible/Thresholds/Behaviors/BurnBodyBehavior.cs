using Content.Shared.Body.Components;
using Content.Shared.IdentityManagement;
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
        var physicsSystem = system.EntityManager.System<PhysicsSystem>();
        var random = system.Random;

        const float maxWornItemThrowSpeed = 40.0f;

        foreach (var item in inventorySystem.GetHandOrInventoryEntities(bodyId))
        {
            if (!inventorySystem.TryGetContainingSlot(item, out var itemSlot))
            {
                continue;
            }

            if (!inventorySystem.TryUnequip(bodyId, bodyId, itemSlot.Name, force: true))
            {
                continue;
            }

            var throwDirection = random.NextAngle().ToVec();
            var throwSpeed = random.NextFloat() * maxWornItemThrowSpeed;
            var throwRotationSpeed = random.NextFloat() * maxWornItemThrowSpeed / 10.0f;

            physicsSystem.ApplyLinearImpulse(item, throwDirection * throwSpeed);
            physicsSystem.ApplyAngularImpulse(item, throwRotationSpeed);
        }

        var bodyIdentity = Identity.Entity(bodyId, system.EntityManager);
        sharedPopupSystem.PopupCoordinates(Loc.GetString("bodyburn-text-others", ("name", bodyIdentity)), transformSystem.GetMoverCoordinates(bodyId), PopupType.LargeCaution);

        system.EntityManager.QueueDeleteEntity(bodyId);
    }
}
