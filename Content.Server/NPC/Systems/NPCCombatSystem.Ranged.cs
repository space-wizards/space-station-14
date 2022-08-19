using Content.Server.NPC.Components;
using Content.Server.Weapon.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    // TODO: Don't predict for hitscan
    private const float ShootSpeed = 20f;

    /// <summary>
    /// Cooldown on raycasting to check LOS.
    /// </summary>
    private const float UnoccludedCooldown = 0.2f;

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
                comp.ShootAccumulator = 0f;
                continue;
            }

            if (targetXform.MapID != xform.MapID)
            {
                comp.Status = CombatStatus.TargetUnreachable;
                comp.ShootAccumulator = 0f;
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
                comp.ShootAccumulator = 0f;
                continue;
            }

            comp.LOSAccumulator += frameTime;

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform, xformQuery);

            // We'll work out the projected spot of the target and shoot there instead of where they are.
            var distance = (targetPos - worldPos).Length;
            var oldInLos = comp.TargetInLOS;

            if (comp.LOSAccumulator > UnoccludedCooldown)
            {
                comp.LOSAccumulator -= UnoccludedCooldown;
                comp.TargetInLOS = _interaction.InRangeUnobstructed(comp.Owner, comp.Target, distance);
            }

            if (!comp.TargetInLOS)
            {
                comp.ShootAccumulator = 0f;
                continue;
            }

            if (!oldInLos && comp.SoundTargetInLOS != null)
            {
                _audio.PlayPvs(comp.SoundTargetInLOS, comp.Owner);
            }

            comp.ShootAccumulator += frameTime;

            if (comp.ShootAccumulator < comp.ShootDelay)
            {
                continue;
            }

            var mapVelocity = targetBody.LinearVelocity;
            var targetSpot = targetPos + mapVelocity * distance / ShootSpeed;

            // If we have a max rotation speed then do that.
            var goalRotation = (targetSpot - worldPos).ToWorldAngle();
            var rotationSpeed = comp.RotationSpeed;

            // We'll rotate even if we can't shoot, looks better.
            if (rotationSpeed != null)
            {
                var rotationDiff = Angle.ShortestDistance(worldRot, goalRotation).Theta;
                var maxRotate = rotationSpeed.Value.Theta * frameTime;

                if (Math.Abs(rotationDiff) > maxRotate)
                {
                    var goalTheta = worldRot + Math.Sign(rotationDiff) * maxRotate;
                    _transform.SetWorldRotation(xform, goalTheta);
                    rotationDiff = (goalRotation - goalTheta);

                    if (Math.Abs(rotationDiff) > comp.AccuracyThreshold)
                    {
                        continue;
                    }
                }
                else
                {
                    _transform.SetWorldRotation(xform, goalRotation);
                }
            }
            else
            {
                _transform.SetWorldRotation(xform, goalRotation);
            }

            // TODO: LOS
            // TODO: Ammo checks
            // TODO: Burst fire
            // TODO: Cycling
            // Max rotation speed

            // TODO: Check if we can face

            if (!_gun.CanShoot(gun))
                continue;

            // TODO: Need CanShoot or something for firerate
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
