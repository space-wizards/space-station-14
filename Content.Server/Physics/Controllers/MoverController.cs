#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Sound;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Physics.Controllers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);

            foreach (var (mobMover, mover, physics) in ComponentManager.EntityQuery<IMobMoverComponent, IMoverComponent, PhysicsComponent>())
            {
                // TODO: Shitcodey and should just be run before all maps
                if (mover.Owner.Transform.MapID != map.MapId) continue;

                HandleMobMovement(mover, physics, mobMover);
            }

            foreach (var (mover, physics) in ComponentManager.EntityQuery<IMoverComponent, PhysicsComponent>())
            {
                if (mover.Owner.HasComponent<IMobMoverComponent>()) continue;

                // TODO: Shitcodey and should just be run before all maps
                if (mover.Owner.Transform.MapID != map.MapId) continue;

                HandleKinematicMovement(mover, physics);
            }
        }

        protected override void HandleFootsteps(IMoverComponent mover, IMobMoverComponent mobMover)
        {
            var transform = mover.Owner.Transform;
            // Handle footsteps.
            if (_mapManager.GridExists(mobMover.LastPosition.GetGridId(EntityManager)))
            {
                // Can happen when teleporting between grids.
                if (!transform.Coordinates.TryDistance(EntityManager, mobMover.LastPosition, out var distance))
                {
                    mobMover.LastPosition = transform.Coordinates;
                    return;
                }

                mobMover.StepSoundDistance += distance;
            }

            mobMover.LastPosition = transform.Coordinates;
            float distanceNeeded;
            if (mover.Sprinting)
            {
                distanceNeeded = StepSoundMoveDistanceRunning;
            }
            else
            {
                distanceNeeded = StepSoundMoveDistanceWalking;
            }

            if (mobMover.StepSoundDistance > distanceNeeded)
            {
                mobMover.StepSoundDistance = 0;

                if (!mover.Owner.HasComponent<FootstepSoundComponent>())
                {
                    return;
                }

                if (mover.Owner.TryGetComponent<InventoryComponent>(out var inventory)
                    && inventory.TryGetSlotItem<ItemComponent>(EquipmentSlotDefines.Slots.SHOES, out var item)
                    && item.Owner.TryGetComponent<FootstepModifierComponent>(out var modifier))
                {
                    modifier.PlayFootstep();
                }
                else
                {
                    PlayFootstepSound(transform.Coordinates, mover.Sprinting);
                }
            }
        }

        private void PlayFootstepSound(EntityCoordinates coordinates, bool sprinting)
        {
            // Step one: figure out sound collection prototype.
            var grid = _mapManager.GetGrid(coordinates.GetGridId(EntityManager));
            var tile = grid.GetTileRef(coordinates);

            // If the coordinates have a FootstepModifier component
            // i.e. component that emit sound on footsteps emit that sound
            string? soundCollectionName = null;
            foreach (var maybeFootstep in grid.GetSnapGridCell(tile.GridIndices, SnapGridOffset.Center))
            {
                if (maybeFootstep.Owner.TryGetComponent(out FootstepModifierComponent? footstep))
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
                if (def.FootstepSounds == null)
                {
                    // Nothing to play, oh well.
                    return;
                }

                soundCollectionName = def.FootstepSounds;
            }

            // Ok well we know the position of the
            try
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
                var file = _robustRandom.Pick(soundCollection.PickFiles);
                _audioSystem.PlayAtCoords(file, coordinates, sprinting ? AudioParams.Default.WithVolume(0.75f) : null);
            }
            catch (UnknownPrototypeException)
            {
                // Shouldn't crash over a sound
                Logger.ErrorS("sound", $"Unable to find sound collection for {soundCollectionName}");
            }
        }

    }
}
