#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.MachineLinking;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.MachineLinking;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorComponent : Component, ISignalReceiver<TwoWayLeverSignal>, ISignalReceiver<bool>
    {
        public override string Name => "Conveyor";

        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        /// <summary>
        ///     The angle to move entities by in relation to the owner's rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("angle")]
        private Angle _angle = Angle.Zero;

        public float Speed => _speed;

        /// <summary>
        ///     The amount of units to move the entity by per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        private float _speed = 2f;

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
                UpdateAppearance();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerChanged(powerChanged);
                    break;
            }
        }

        private void OnPowerChanged(PowerChangedMessage e)
        {
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                if (Powered)
                {
                    appearance.SetData(ConveyorVisuals.State, _state);
                }
                else
                {
                    appearance.SetData(ConveyorVisuals.State, ConveyorState.Off);
                }
            }
        }

        /// <summary>
        ///     Calculates the angle in which entities on top of this conveyor
        ///     belt are pushed in
        /// </summary>
        /// <returns>
        ///     The angle when taking into account if the conveyor is reversed
        /// </returns>
        public Angle GetAngle()
        {
            var adjustment = _state == ConveyorState.Reversed ? MathHelper.Pi : 0;
            var radians = MathHelper.DegreesToRadians(_angle);

            return new Angle(Owner.Transform.LocalRotation.Theta + radians + adjustment);
        }

        public bool CanRun()
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

        public bool CanMove(IEntity entity)
        {
            // TODO We should only check status InAir or Static or MapGrid or /mayber/ container
            if (entity == Owner)
            {
                return false;
            }

            if (!entity.TryGetComponent(out IPhysBody? physics) ||
                physics.BodyType == BodyType.Static)
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

            if (entity.IsInContainer())
            {
                return false;
            }

            return true;
        }

        public void TriggerSignal(TwoWayLeverSignal signal)
        {
            State = signal switch
            {
                TwoWayLeverSignal.Left => ConveyorState.Reversed,
                TwoWayLeverSignal.Middle => ConveyorState.Off,
                TwoWayLeverSignal.Right => ConveyorState.Forward,
                _ => ConveyorState.Off
            };
        }

        public void TriggerSignal(bool signal)
        {
            State = signal ? ConveyorState.Forward : ConveyorState.Off;
        }
    }
}
