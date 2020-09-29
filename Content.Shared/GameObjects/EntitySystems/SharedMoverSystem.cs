#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Content.Shared.Physics.Pull;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedMoverSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            var moveUpCmdHandler = new MoverDirInputCmdHandler(Direction.North);
            var moveLeftCmdHandler = new MoverDirInputCmdHandler(Direction.West);
            var moveRightCmdHandler = new MoverDirInputCmdHandler(Direction.East);
            var moveDownCmdHandler = new MoverDirInputCmdHandler(Direction.South);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
                .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
                .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
                .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
                .Bind(EngineKeyFunctions.Walk, new WalkInputCmdHandler())
                .Register<SharedMoverSystem>();

            _configurationManager.RegisterCVar("game.diagonalmovement", true, CVar.ARCHIVE);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedMoverSystem>();
            base.Shutdown();
        }

        protected void UpdateKinematics(ITransformComponent transform, IMoverComponent mover, ICollidableComponent collidable)
        {
            collidable.EnsureController<MoverController>();

            var weightless = !transform.Owner.HasComponent<MovementIgnoreGravityComponent>() &&
                             _physicsManager.IsWeightless(transform.Coordinates);

            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(transform, mover, collidable);

                if (!touching)
                {
                    return;
                }
            }

            // TODO: movement check.
            var (walkDir, sprintDir) = mover.VelocityDir;
            var combined = walkDir + sprintDir;
            if (combined.LengthSquared < 0.001 || !ActionBlockerSystem.CanMove(mover.Owner) && !weightless)
            {
                if (collidable.TryGetController(out MoverController controller))
                {
                    controller.StopMoving();
                }
            }
            else
            {
                if (weightless)
                {
                    if (collidable.TryGetController(out MoverController controller))
                    {
                        controller.Push(combined, mover.CurrentPushSpeed);
                    }

                    transform.LocalRotation = walkDir.GetDir().ToAngle();
                    return;
                }

                var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;

                {
                    if (collidable.TryGetController(out MoverController controller))
                    {
                        controller.Move(total, 1);
                    }
                }

                transform.LocalRotation = total.GetDir().ToAngle();

                HandleFootsteps(mover);
            }
        }

        protected virtual void HandleFootsteps(IMoverComponent mover)
        {

        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            ICollidableComponent collider)
        {
            foreach (var entity in _entityManager.GetEntitiesInRange(transform.Owner, mover.GrabRange, true))
            {
                if (entity == transform.Owner)
                {
                    continue; // Don't try to push off of yourself!
                }

                if (!entity.TryGetComponent<ICollidableComponent>(out var otherCollider))
                {
                    continue;
                }

                // Don't count pulled entities
                if (otherCollider.HasController<PullController>())
                {
                    continue;
                }

                // TODO: Item check.
                var touching = ((collider.CollisionMask & otherCollider.CollisionLayer) != 0x0
                                || (otherCollider.CollisionMask & collider.CollisionLayer) != 0x0) // Ensure collision
                               && !entity.HasComponent<IItemComponent>(); // This can't be an item

                if (touching)
                {
                    return true;
                }
            }

            return false;
        }


        private static void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
        {
            if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
                return;

            var owner = session?.AttachedEntity;

            if (owner != null)
            {
                foreach (var comp in owner.GetAllComponents<IRelayMoveInput>())
                {
                    comp.MoveInputPressed(session);
                }
            }

            moverComp.SetVelocityDirection(dir, subTick, state);
        }

        private static void HandleRunChange(ICommonSession? session, ushort subTick, bool walking)
        {
            if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
            {
                return;
            }

            moverComp.SetSprinting(subTick, walking);
        }

        private static bool TryGetAttachedComponent<T>(ICommonSession? session, [MaybeNullWhen(false)] out T component)
            where T : class, IComponent
        {
            component = default;

            var ent = session?.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T? comp))
                return false;

            component = comp;
            return true;
        }

        private sealed class MoverDirInputCmdHandler : InputCmdHandler
        {
            private readonly Direction _dir;

            public MoverDirInputCmdHandler(Direction dir)
            {
                _dir = dir;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage full))
                {
                    return false;
                }

                HandleDirChange(session, _dir, message.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }

        private sealed class WalkInputCmdHandler : InputCmdHandler
        {
            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage full))
                {
                    return false;
                }

                HandleRunChange(session, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }
    }
}
