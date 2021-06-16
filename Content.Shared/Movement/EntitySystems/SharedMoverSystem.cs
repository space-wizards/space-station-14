#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.MobState;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Shared.Movement.EntitySystems
{
    /// <summary>
    ///     Handles converting inputs into movement.
    /// </summary>
    public sealed class SharedMoverSystem : EntitySystem
    {
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
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedMoverSystem>();
            base.Shutdown();
        }

        private static void HandleDirChange(ICommonSession? session, Direction dir, ushort subTick, bool state)
        {
            if (!TryGetAttachedComponent<IMoverComponent>(session, out var moverComp))
                return;

            var owner = session?.AttachedEntity;

            if (owner != null && session != null)
            {
                foreach (var comp in owner.GetAllComponents<IRelayMoveInput>())
                {
                    comp.MoveInputPressed(session);
                }

                // For stuff like "Moving out of locker" or the likes
                if (owner.IsInContainer() &&
                    (!owner.TryGetComponent(out IMobStateComponent? mobState) ||
                     mobState.IsAlive()))
                {
                    var relayEntityMoveMessage = new RelayMovementEntityMessage(owner);
                    owner.Transform.Parent!.Owner.SendMessage(owner.Transform, relayEntityMoveMessage);
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

        private static bool TryGetAttachedComponent<T>(ICommonSession? session, [NotNullWhen(true)] out T? component)
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
                if (message is not FullInputCmdMessage full)
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
                if (message is not FullInputCmdMessage full)
                {
                    return false;
                }

                HandleRunChange(session, full.SubTick, full.State == BoundKeyState.Down);
                return false;
            }
        }
    }
}
