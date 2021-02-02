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
using Content.Shared.Physics.Controllers;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Physics.Controllers
{
    public class MobMoverController : SharedMobMoverController
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

        public override void UpdateBeforeSolve(float frameTime)
        {
            base.UpdateBeforeSolve(frameTime);
            foreach (var (mover, physics) in ComponentManager.EntityQuery<SharedPlayerInputMoverComponent, PhysicsComponent>(false))
            {
                UpdateKinematics(frameTime, mover.Owner.Transform, mover, physics);
            }

            foreach (var (mover, physics) in ComponentManager.EntityQuery<AiControllerComponent, PhysicsComponent>(false))
            {
                UpdateKinematics(frameTime, mover.Owner.Transform, mover, physics);
            }
        }

        protected override void HandleFootsteps(IMoverComponent mover)
        {
            var transform = mover.Owner.Transform;
            // Handle footsteps.
            if (_mapManager.GridExists(mover.LastPosition.GetGridId(EntityManager)))
            {
                // Can happen when teleporting between grids.
                if (!transform.Coordinates.TryDistance(EntityManager, mover.LastPosition, out var distance))
                {
                    mover.LastPosition = transform.Coordinates;
                    return;
                }

                mover.StepSoundDistance += distance;
            }

            mover.LastPosition = transform.Coordinates;
            float distanceNeeded;
            if (mover.Sprinting)
            {
                distanceNeeded = StepSoundMoveDistanceRunning;
            }
            else
            {
                distanceNeeded = StepSoundMoveDistanceWalking;
            }

            if (mover.StepSoundDistance > distanceNeeded)
            {
                mover.StepSoundDistance = 0;

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
                    PlayFootstepSound(transform.Coordinates);
                }
            }
        }

        private void PlayFootstepSound(EntityCoordinates coordinates)
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
                _audioSystem.PlayAtCoords(file, coordinates);
            }
            catch (UnknownPrototypeException)
            {
                // Shouldn't crash over a sound
                Logger.ErrorS("sound", $"Unable to find sound collection for {soundCollectionName}");
            }
        }

    }
}
