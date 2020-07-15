using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components.Conveyor;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Conveyor
{
    [RegisterComponent]
    public class ConveyorComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        public override string Name => "Conveyor";

        /// <summary>
        ///     The angle in radians to move entities by in relation
        ///     to the owner's rotation.
        ///     Parsed from YAML as degrees.
        /// </summary>
        [ViewVariables]
        private double _angle;

        /// <summary>
        ///     The amount of units to move the entity by.
        /// </summary>
        [ViewVariables]
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

                if (!Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    return;
                }

                appearance.SetData(ConveyorVisuals.State, value);
            }
        }

        /// <summary>
        ///     Calculates the angle in which entities on top of this conveyor belt
        ///     are pushed in
        /// </summary>
        /// <returns>
        ///     The angle when taking into account if the conveyor is reversed
        /// </returns>
        private Angle GetAngle()
        {
            var adjustment = _state == ConveyorState.Reversed ? MathHelper.Pi : 0;

            return new Angle(Owner.Transform.LocalRotation.Theta + _angle + adjustment);
        }

        private bool CanRun()
        {
            if (State == ConveyorState.Off)
            {
                return false;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent receiver) &&
                !receiver.Powered)
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

            if (entity.TryGetComponent(out PhysicsComponent physics) &&
                physics.Anchored)
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

            return true;
        }

        public void ChangeState(ConveyorState state)
        {
            State = state;
        }

        public void Update(float frameTime)
        {
            if (!CanRun())
            {
                return;
            }

            var intersecting = _entityManager.GetEntitiesIntersecting(Owner, true);

            foreach (var entity in intersecting)
            {
                if (!CanMove(entity))
                {
                    continue;
                }

                entity.Transform.WorldPosition += GetAngle().ToVec() * _speed * frameTime;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            double degrees = 0;
            serializer.DataField(ref degrees, "angle", 0);

            _angle = MathHelper.DegreesToRadians(degrees);

            serializer.DataField(ref _speed, "speed", 2);
        }
    }
}
