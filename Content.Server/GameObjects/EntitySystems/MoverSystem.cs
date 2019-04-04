using System;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Content.Shared.Audio;
using Content.Shared.Maps;
using JetBrains.Annotations;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Timing;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;
using SS14.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class MoverSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency]
        private IPauseManager _pauseManager;
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private Random _footstepRandom;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        /// <inheritdoc />
        public override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            EntityQuery = new TypeEntityQuery(typeof(PlayerInputMoverComponent));
            
            var moveUpCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.North, true),
                session => HandleDirChange(session, Direction.North, false));
            var moveLeftCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.West, true),
                session => HandleDirChange(session, Direction.West, false));
            var moveRightCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.East, true),
                session => HandleDirChange(session, Direction.East, false));
            var moveDownCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.South, true),
                session => HandleDirChange(session, Direction.South, false));
            var runCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleRunChange(session, true),
                session => HandleRunChange(session, false));

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();

            input.BindMap.BindFunction(EngineKeyFunctions.MoveUp, moveUpCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveRight, moveRightCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveDown, moveDownCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.Run, runCmdHandler);

            SubscribeEvent<PlayerAttachSystemMessage>(PlayerAttached);
            SubscribeEvent<PlayerDetachedSystemMessage>(PlayerDetached);

            _footstepRandom = new Random();
            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();
        }

        private static void PlayerAttached(object sender, PlayerAttachSystemMessage ev)
        {
            if (ev.Entity.HasComponent<IMoverComponent>())
            {
                ev.Entity.RemoveComponent<IMoverComponent>();
            }
            ev.Entity.AddComponent<PlayerInputMoverComponent>();
        }

        private static void PlayerDetached(object sender, PlayerDetachedSystemMessage ev)
        {
            ev.Entity.RemoveComponent<PlayerInputMoverComponent>();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (EntitySystemManager.TryGetEntitySystem(out InputSystem input))
            {
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveUp);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveLeft);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveRight);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveDown);
                input.BindMap.UnbindFunction(EngineKeyFunctions.Run);
            }

            base.Shutdown();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                var mover = entity.GetComponent<PlayerInputMoverComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();

                UpdateKinematics(entity.Transform, mover, physics);
            }
        }

        private void UpdateKinematics(ITransformComponent transform, PlayerInputMoverComponent mover, PhysicsComponent physics)
        {
            if (mover.VelocityDir.LengthSquared < 0.001 || !ActionBlockerSystem.CanMove(mover.Owner))
            {
                if (physics.LinearVelocity != Vector2.Zero)
                    physics.LinearVelocity = Vector2.Zero;
            }
            else
            {
                physics.LinearVelocity = mover.VelocityDir * (mover.Sprinting ? mover.SprintMoveSpeed : mover.WalkMoveSpeed);
                transform.LocalRotation = mover.VelocityDir.GetDir().ToAngle();

                // Handle footsteps.
                var distance = transform.GridPosition.Distance(mover.LastPosition);
                mover.StepSoundDistance += distance;
                mover.LastPosition = transform.GridPosition;
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
                    PlayFootstepSound(transform.GridPosition);
                }
            }
        }

        private static void HandleDirChange(ICommonSession session, Direction dir, bool state)
        {
            if(!TryGetAttachedComponent(session as IPlayerSession, out PlayerInputMoverComponent moverComp))
                return;

            moverComp.SetVelocityDirection(dir, state);
        }

        private static void HandleRunChange(ICommonSession session, bool running)
        {
            if(!TryGetAttachedComponent(session as IPlayerSession, out PlayerInputMoverComponent moverComp))
                return;

            moverComp.Sprinting = running;
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T: Component
        {
            component = default;
            
            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private void PlayFootstepSound(GridCoordinates coordinates)
        {
            // Step one: figure out sound collection prototype.
            var grid = coordinates.Grid;
            var tile = grid.GetTile(coordinates);

            // If the coordinates have a catwalk, it's always catwalk.
            string soundCollectionName;
            var catwalk = false;
            foreach (var maybeCatwalk in grid.GetSnapGridCell(tile.GridTile, SnapGridOffset.Center))
            {
                if (maybeCatwalk.Owner.HasComponent<CatwalkComponent>())
                {
                    catwalk = true;
                    break;
                }
            }

            if (catwalk)
            {
                // Catwalk overrides tile sound.s
                soundCollectionName = "footstep_catwalk";
            }
            else
            {
                // Walking on a tile.
                var def = (ContentTileDefinition)tile.TileDef;
                if (def.FootstepSounds == null)
                {
                    // Nothing to play, oh well.
                    return;
                }
                soundCollectionName = def.FootstepSounds;
            }

            // Ok well we know the position of the
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(soundCollectionName);
            var file = _footstepRandom.Pick(soundCollection.PickFiles);
            _audioSystem.Play(file, coordinates);
        }
    }
}
