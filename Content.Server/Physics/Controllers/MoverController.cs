#nullable enable
using System.Collections.Generic;
using Content.Server.Actions;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Sound;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Tag;
using Content.Shared.Maps;
using Content.Shared.Physics.Controllers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private AudioSystem _audioSystem = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private HashSet<EntityUid> _excludedMobs = new();

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            _excludedMobs.Clear();

            foreach (var (mobMover, mover, physics) in ComponentManager.EntityQuery<IMobMoverComponent, IMoverComponent, PhysicsComponent>())
            {
                _excludedMobs.Add(mover.Owner.Uid);
                HandleMobMovement(mover, physics, mobMover);
            }

            foreach (var mover in ComponentManager.EntityQuery<ShuttleControllerComponent>())
            {
                _excludedMobs.Add(mover.Owner.Uid);
                HandleShuttleMovement(mover);
            }

            foreach (var (mover, physics) in ComponentManager.EntityQuery<IMoverComponent, PhysicsComponent>(true))
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
        private void HandleShuttleMovement(ShuttleControllerComponent mover)
        {
            var gridId = mover.Owner.Transform.GridID;

            if (!_mapManager.TryGetGrid(gridId, out var grid) || !EntityManager.TryGetEntity(grid.GridEntityId, out var gridEntity)) return;

            //TODO: Switch to shuttle component
            if (!gridEntity.TryGetComponent(out PhysicsComponent? physics))
            {
                physics = gridEntity.AddComponent<PhysicsComponent>();
                physics.BodyStatus = BodyStatus.InAir;
                physics.CanCollide = true;
                physics.AddFixture(new Fixture(physics, new PhysShapeGrid(grid)));
            }

            // TODO: Uhh this probably doesn't work but I still need to rip out the entity tree and make RenderingTreeSystem use grids so I'm not overly concerned about breaking shuttles.
            physics.ApplyForce(mover.VelocityDir.walking + mover.VelocityDir.sprinting);
            mover.VelocityDir = (Vector2.Zero, Vector2.Zero);
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
            string? soundCollectionName = null;
            foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
            {
                if (EntityManager.ComponentManager.TryGetComponent(maybeFootstep, out FootstepModifierComponent? footstep))
                {
                    soundCollectionName = footstep._soundCollectionName;
                    break;
                }
            }
            // if there is no FootstepModifierComponent, determine sound based on tiles
            if (soundCollectionName == null)
            {
                // Walking on a tile.
                var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
                if (string.IsNullOrEmpty(def.FootstepSounds))
                {
                    // Nothing to play, oh well.
                    return;
                }

                soundCollectionName = def.FootstepSounds;
            }

            if (!_prototypeManager.TryIndex(soundCollectionName, out SoundCollectionPrototype? soundCollection))
            {
                Logger.ErrorS("sound", $"Unable to find sound collection for {soundCollectionName}");
                return;
            }

            SoundSystem.Play(
                Filter.Pvs(coordinates),
                _robustRandom.Pick(soundCollection.PickFiles),
                mover,
                sprinting ? AudioParams.Default.WithVolume(0.75f) : null);
        }
    }
}
