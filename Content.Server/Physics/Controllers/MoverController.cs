using System;
using System.Collections.Generic;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Movement.Components;
using Content.Server.Shuttles;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Sound;
using Content.Shared.Shuttles;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public class MoverController : SharedMoverController
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private float _shuttleDockSpeedCap;

        private HashSet<EntityUid> _excludedMobs = new();

        public override void Initialize()
        {
            base.Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.ShuttleDockSpeedCap, value => _shuttleDockSpeedCap = value, true);
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            _excludedMobs.Clear();

            foreach (var (mobMover, mover, physics) in EntityManager.EntityQuery<IMobMoverComponent, IMoverComponent, PhysicsComponent>())
            {
                _excludedMobs.Add(mover.Owner.Uid);
                HandleMobMovement(mover, physics, mobMover);
            }

            foreach (var (pilot, mover) in EntityManager.EntityQuery<PilotComponent, SharedPlayerInputMoverComponent>())
            {
                if (pilot.Console == null) continue;
                _excludedMobs.Add(mover.Owner.Uid);
                HandleShuttleMovement(mover);
            }

            foreach (var (mover, physics) in EntityManager.EntityQuery<IMoverComponent, PhysicsComponent>(true))
            {
                if (_excludedMobs.Contains(mover.Owner.Uid)) continue;

                HandleKinematicMovement(mover, physics);
            }
        }

        /*
         * Some thoughts:
         * Unreal actually doesn't predict vehicle movement at all, it's purely server-side which I thought was interesting
         * The reason for this is that vehicles change direction very slowly compared to players so you don't really have the requirement for quick movement anyway
         * As such could probably just look at applying a force / impulse to the shuttle server-side only so it controls like the titanic.
         */
        private void HandleShuttleMovement(SharedPlayerInputMoverComponent mover)
        {
            var gridId = mover.Owner.Transform.GridID;

            if (!_mapManager.TryGetGrid(gridId, out var grid) || !EntityManager.TryGetEntity(grid.GridEntityId, out var gridEntity)) return;

            if (!gridEntity.TryGetComponent(out ShuttleComponent? shuttleComponent) ||
                !gridEntity.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            // Depending whether you have "cruise" mode on (tank controls, higher speed) or "docking" mode on (strafing, lower speed)
            // inputs will do different things.
            // TODO: Do that
            float speedCap;
            // This is comically fast for debugging
            var angularSpeed = 20000f;

            // ShuttleSystem has already worked out the ratio so we'll just multiply it back by the mass.
            var movement = (mover.VelocityDir.walking + mover.VelocityDir.sprinting);

            switch (shuttleComponent.Mode)
            {
                case ShuttleMode.Docking:
                    if (movement.Length != 0f)
                        physicsComponent.ApplyLinearImpulse(physicsComponent.Owner.Transform.WorldRotation.RotateVec(movement) * shuttleComponent.SpeedMultipler * physicsComponent.Mass);

                    speedCap = _shuttleDockSpeedCap;
                    break;
                case ShuttleMode.Cruise:
                    if (movement.Length != 0.0f)
                    {
                        // Currently this is slow BUT we'd have a separate multiplier for docking and cruising or whatever.
                        physicsComponent.ApplyLinearImpulse((physicsComponent.Owner.Transform.WorldRotation + new Angle(MathF.PI / 2)).ToVec() *
                                                            shuttleComponent.SpeedMultipler *
                                                            physicsComponent.Mass *
                                                            movement.Y *
                                                            10);
                        physicsComponent.ApplyAngularImpulse(-movement.X * angularSpeed);
                    }

                    // TODO WHEN THIS ACTUALLY WORKS
                    speedCap = _shuttleDockSpeedCap * 10;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Look don't my ride ass on this stuff most of the PR was just getting the thing working, we can
            // ideaguys the shit out of it later.

            var velocity = physicsComponent.LinearVelocity;

            if (velocity.Length < 0.1f && movement.Length == 0f)
            {
                physicsComponent.LinearVelocity = Vector2.Zero;
                return;
            }

            if (velocity.Length > speedCap)
            {
                physicsComponent.LinearVelocity = velocity.Normalized * speedCap;
            }
        }

        protected override void HandleFootsteps(IMoverComponent mover, IMobMoverComponent mobMover)
        {
            if (!mover.Owner.HasTag("FootstepSound")) return;

            var transform = mover.Owner.Transform;
            var coordinates = transform.Coordinates;
            var gridId = coordinates.GetGridId(EntityManager);
            var distanceNeeded = mover.Sprinting ? StepSoundMoveDistanceRunning : StepSoundMoveDistanceWalking;

            // Handle footsteps.
            if (_mapManager.GridExists(gridId))
            {
                // Can happen when teleporting between grids.
                if (!coordinates.TryDistance(EntityManager, mobMover.LastPosition, out var distance) ||
                    distance > distanceNeeded)
                {
                    mobMover.StepSoundDistance = distanceNeeded;
                }
                else
                {
                    mobMover.StepSoundDistance += distance;
                }
            }
            else
            {
                // In space no one can hear you squeak
                return;
            }

            DebugTools.Assert(gridId != GridId.Invalid);
            mobMover.LastPosition = coordinates;

            if (mobMover.StepSoundDistance < distanceNeeded) return;

            mobMover.StepSoundDistance -= distanceNeeded;

            if (mover.Owner.TryGetComponent<InventoryComponent>(out var inventory)
                && inventory.TryGetSlotItem<ItemComponent>(EquipmentSlotDefines.Slots.SHOES, out var item)
                && item.Owner.TryGetComponent<FootstepModifierComponent>(out var modifier))
            {
                modifier.PlayFootstep();
            }
            else
            {
                PlayFootstepSound(mover.Owner, gridId, coordinates, mover.Sprinting);
            }
        }

        private void PlayFootstepSound(IEntity mover, GridId gridId, EntityCoordinates coordinates, bool sprinting)
        {
            var grid = _mapManager.GetGrid(gridId);
            var tile = grid.GetTileRef(coordinates);

            if (tile.IsSpace(_tileDefinitionManager)) return;

            // If the coordinates have a FootstepModifier component
            // i.e. component that emit sound on footsteps emit that sound
            string? soundToPlay = null;
            foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
            {
                if (EntityManager.TryGetComponent(maybeFootstep, out FootstepModifierComponent? footstep))
                {
                    soundToPlay = footstep.SoundCollection.GetSound();
                    break;
                }
            }
            // if there is no FootstepModifierComponent, determine sound based on tiles
            if (soundToPlay == null)
            {
                // Walking on a tile.
                var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
                soundToPlay = def.FootstepSounds.GetSound();
                if (string.IsNullOrEmpty(soundToPlay))
                    return;
            }

            if (string.IsNullOrWhiteSpace(soundToPlay))
            {
                Logger.ErrorS("sound", $"Unable to find sound in {nameof(PlayFootstepSound)}");
                return;
            }

            SoundSystem.Play(
                Filter.Pvs(coordinates),
                soundToPlay,
                mover.Transform.Coordinates,
                sprinting ? AudioParams.Default.WithVolume(0.75f) : null);
        }
    }
}
