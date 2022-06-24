using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Shared.Movement.Systems
{
    /// <summary>
    ///     Handles converting inputs into movement.
    /// </summary>
    public abstract partial class SharedMoverController
    {
        private void InitializeInput()
        {
            var moveUpCmdHandler = new MoverDirInputCmdHandler(this, Direction.North);
            var moveLeftCmdHandler = new MoverDirInputCmdHandler(this, Direction.West);
            var moveRightCmdHandler = new MoverDirInputCmdHandler(this, Direction.East);
            var moveDownCmdHandler = new MoverDirInputCmdHandler(this, Direction.South);

            CommandBinds.Builder
                .Bind(EngineKeyFunctions.MoveUp, moveUpCmdHandler)
                .Bind(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler)
                .Bind(EngineKeyFunctions.MoveRight, moveRightCmdHandler)
                .Bind(EngineKeyFunctions.MoveDown, moveDownCmdHandler)
                .Bind(EngineKeyFunctions.Walk, new WalkInputCmdHandler(this))
                .Register<SharedMoverController>();
        }

        private void ShutdownInput()
        {
            CommandBinds.Unregister<SharedMoverController>();
        }

        private void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
        {
            if (!TryComp<IMoverComponent>(session?.AttachedEntity, out var moverComp))
                return;

            var owner = session?.AttachedEntity;

            if (owner != null && session != null)
            {
                EntityManager.EventBus.RaiseLocalEvent(owner.Value, new RelayMoveInputEvent(session), true);

                // For stuff like "Moving out of locker" or the likes
                if (owner.Value.IsInContainer() &&
                    (!EntityManager.TryGetComponent(owner.Value, out MobStateComponent? mobState) ||
                     mobState.IsAlive()))
                {
                    var relayMoveEvent = new RelayMovementEntityEvent(owner.Value);
                    EntityManager.EventBus.RaiseLocalEvent(EntityManager.GetComponent<TransformComponent>(owner.Value).ParentUid, relayMoveEvent, true);
                }
                // Pass the rider's inputs to the vehicle (the rider itself is on the ignored list in C.S/MoverController.cs)
                if (TryComp<RiderComponent>(owner.Value, out var rider) && rider.Vehicle != null && rider.Vehicle.HasKey)
                {
                    if (TryComp<IMoverComponent>(rider.Vehicle.Owner, out var vehicleMover))
                    {
                        vehicleMover.SetVelocityDirection(dir, subTick, state);
                    }
                }
            }

            moverComp.SetVelocityDirection(dir, subTick, state);
        }

        private void HandleRunChange(ICommonSession? session, ushort subTick, bool walking)
        {
            if (!TryComp<IMoverComponent>(session?.AttachedEntity, out var moverComp))
            {
                return;
            }

            moverComp.SetSprinting(subTick, walking);
        }

        private sealed class MoverDirInputCmdHandler : InputCmdHandler
        {
            private SharedMoverController _controller;
            private readonly Direction _dir;

            public MoverDirInputCmdHandler(SharedMoverController controller, Direction dir)
            {
                _controller = controller;
                _dir = dir;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full) return false;

                _controller.HandleDirChange(session, _dir, message.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }

        private sealed class WalkInputCmdHandler : InputCmdHandler
        {
            private SharedMoverController _controller;

            public WalkInputCmdHandler(SharedMoverController controller)
            {
                _controller = controller;
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (message is not FullInputCmdMessage full) return false;

                _controller.HandleRunChange(session, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }
    }
}
