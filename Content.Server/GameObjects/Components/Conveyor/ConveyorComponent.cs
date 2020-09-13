#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Conveyor";

        /// <summary>
        ///     The angle to move entities by in relation to the owner's rotation.
        /// </summary>
        [ViewVariables]
        private Angle _angle;

        /// <summary>
        ///     The amount of units to move the entity by per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _speed;

        private ConveyorState _state;

        /// <summary>
        ///     The current state of this conveyor
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private ConveyorState State
        {
            get => _state;
            set
            {
                _state = value;

                if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    return;
                }

                appearance.SetData(ConveyorVisuals.State, value);
            }
        }

        private ConveyorGroup? _group = new ConveyorGroup();

        /// <summary>
        ///     Calculates the angle in which entities on top of this conveyor
        ///     belt are pushed in
        /// </summary>
        /// <returns>
        ///     The angle when taking into account if the conveyor is reversed
        /// </returns>
        private Angle GetAngle()
        {
            var adjustment = _state == ConveyorState.Reversed ? MathHelper.Pi : 0;
            var radians = MathHelper.DegreesToRadians(_angle);

            return new Angle(Owner.Transform.LocalRotation.Theta + radians + adjustment);
        }

        private bool CanRun()
        {
            if (State == ConveyorState.Off)
            {
                return false;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            return true;
        }

        private bool CanMove(IEntity entity)
        {
            if (entity == Owner)
            {
                return false;
            }

            if (!entity.TryGetComponent(out ICollidableComponent? collidable) ||
                collidable.Anchored)
            {
                return false;
            }

            if (entity.HasComponent<ConveyorComponent>())
            {
                return false;
            }

            if (entity.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            if (ContainerHelpers.IsInContainer(entity))
            {
                return false;
            }

            return true;
        }

        public void Update(float frameTime)
        {
            if (!CanRun())
            {
                return;
            }

            var intersecting = _entityManager.GetEntitiesIntersecting(Owner, true);
            var direction = GetAngle().ToVec();

            foreach (var entity in intersecting)
            {
                if (!CanMove(entity))
                {
                    continue;
                }

                if (entity.TryGetComponent(out ICollidableComponent? collidable))
                {
                    var controller = collidable.EnsureController<ConveyedController>();
                    controller.Move(direction, _speed * frameTime);
                }
            }
        }

        private async Task<bool> ToolUsed(IEntity user, ToolComponent tool)
        {
            if (!Owner.HasComponent<ItemComponent>() &&
                await tool.UseTool(user, Owner, 0.5f, ToolQuality.Prying))
            {
                State = ConveyorState.Loose;

                Owner.AddComponent<ItemComponent>();
                _group?.RemoveConveyor(this);
                Owner.RandomOffset(0.2f);

                return true;
            }

            return false;
        }

        public void Sync(ConveyorGroup group)
        {
            _group = group;

            if (State == ConveyorState.Loose)
            {
                return;
            }

            State = group.State == ConveyorState.Loose
                ? ConveyorState.Off
                : group.State;
        }

        /// <summary>
        ///     Disconnects this conveyor from any switch.
        /// </summary>
        private void Disconnect()
        {
            _group?.RemoveConveyor(this);
            _group = null;
            State = ConveyorState.Off;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "switches",
                new List<EntityUid>(),
                ids =>
                {
                    if (ids == null)
                    {
                        return;
                    }

                    foreach (var id in ids)
                    {
                        if (!Owner.EntityManager.TryGetEntity(id, out var @switch))
                        {
                            continue;
                        }

                        if (!@switch.TryGetComponent(out ConveyorSwitchComponent? component))
                        {
                            continue;
                        }

                        component.Connect(this);
                    }
                },
                () => _group?.Switches.Select(@switch => @switch.Owner.Uid).ToList());

            serializer.DataField(ref _angle, "angle", 0);
            serializer.DataField(ref _speed, "speed", 2);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Disconnect();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent(out ConveyorSwitchComponent? conveyorSwitch))
            {
                conveyorSwitch.Connect(this, eventArgs.User);
                return true;
            }

            if (eventArgs.Using.TryGetComponent(out ToolComponent? tool))
            {
                return await ToolUsed(eventArgs.User, tool);
            }

            return false;
        }
    }
}
