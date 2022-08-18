using Content.Server.NPC.Components;
using Content.Server.Weapon.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly RotateToFaceSystem _face = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // TODO: Don't predict for hitscan
    private const float ShootSpeed = 20f;

    private void InitializeRanged()
    {

    }

    private void UpdateRanged(float frameTime)
    {
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var combatQuery = GetEntityQuery<SharedCombatModeComponent>();

        foreach (var (comp, xform) in EntityQuery<NPCRangedCombatComponent, TransformComponent>())
        {
            if (!xformQuery.TryGetComponent(comp.Target, out var targetXform) ||
                !bodyQuery.TryGetComponent(comp.Target, out var targetBody))
            {
                comp.Status = CombatStatus.TargetUnreachable;
                continue;
            }

            if (targetXform.MapID != xform.MapID)
            {
                comp.Status = CombatStatus.TargetUnreachable;
                continue;
            }

            if (combatQuery.TryGetComponent(comp.Owner, out var combatMode))
            {
                combatMode.IsInCombatMode = true;
            }

            var gun = _gun.GetGun(comp.Owner);

            if (gun == null)
            {
                comp.Status = CombatStatus.NoWeapon;
                continue;
            }

            // TODO: LOS
            // TODO: Ammo checks
            // TODO: Burst fire
            // TODO: Cycling
            // Max rotation speed
            _face.TryFaceCoordinates(comp.Owner, _transform.GetWorldPosition(targetXform, xformQuery), xform);

            if (!_gun.CanShoot(gun))
                continue;

            // TODO: Need CanShoot or something for firerate

            // We'll work out the projected spot of the target and shoot there instead of where they are.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform, xformQuery);

            var distance = (targetPos - worldPos).Length;
            var mapVelocity = targetBody.LinearVelocity;

            var targetSpot = targetPos + mapVelocity * distance / ShootSpeed;
            EntityCoordinates targetCordinates;

            if (_mapManager.TryFindGridAt(xform.MapID, targetPos, out var mapGrid))
            {
                targetCordinates = new EntityCoordinates(mapGrid.GridEntityId, mapGrid.WorldToLocal(targetSpot));
            }
            else
            {
                targetCordinates = new EntityCoordinates(xform.MapUid!.Value, targetSpot);
            }

            _gun.AttemptShoot(comp.Owner, gun, targetCordinates);
        }
    }
}
