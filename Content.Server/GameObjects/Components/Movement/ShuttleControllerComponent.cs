#nullable enable
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    internal class ShuttleControllerComponent : Component, IMoverComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private bool _movingUp;
        private bool _movingDown;
        private bool _movingLeft;
        private bool _movingRight;

        /// <summary>
        ///     ID of the alert to show when piloting
        /// </summary>
        private AlertType _pilotingAlertType;

        /// <summary>
        ///     The entity that's currently controlling this component.
        ///     Changed from <see cref="SetController"/> and <see cref="RemoveController"/>
        /// </summary>
        private IEntity? _controller;

        public override string Name => "ShuttleController";

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentWalkSpeed { get; } = 8;
        public float CurrentSprintSpeed => 0;

        /// <inheritdoc />
        [ViewVariables]
        public float CurrentPushSpeed => 0.0f;

        /// <inheritdoc />
        [ViewVariables]
        public float GrabRange => 0.0f;

        public bool Sprinting => false;

        public (Vector2 walking, Vector2 sprinting) VelocityDir { get; } = (Vector2.Zero, Vector2.Zero);
        public EntityCoordinates LastPosition { get; set; }
        public float StepSoundDistance { get; set; }

        public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
        {
            var gridId = Owner.Transform.GridID;

            if (_mapManager.TryGetGrid(gridId, out var grid) &&
                Owner.EntityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                //TODO: Switch to shuttle component
                if (!gridEntity.TryGetComponent(out IPhysicsComponent? physics))
                {
                    physics = gridEntity.AddComponent<PhysicsComponent>();
                    physics.Mass = 1;
                    physics.CanCollide = true;
                    physics.PhysicsShapes.Add(new PhysShapeGrid(grid));
                }

                var controller = physics.EnsureController<ShuttleController>();
                controller.Push(CalcNewVelocity(direction, enabled), CurrentWalkSpeed);
            }
        }

        public void SetSprinting(ushort subTick, bool walking)
        {
            // Shuttles can't sprint.
        }

        private Vector2 CalcNewVelocity(Direction direction, bool enabled)
        {
            switch (direction)
            {
                case Direction.East:
                    _movingRight = enabled;
                    break;
                case Direction.North:
                    _movingUp = enabled;
                    break;
                case Direction.West:
                    _movingLeft = enabled;
                    break;
                case Direction.South:
                    _movingDown = enabled;
                    break;
            }

            // key directions are in screen coordinates
            // _moveDir is in world coordinates
            // if the camera is moved, this needs to be changed

            var x = 0;
            x -= _movingLeft ? 1 : 0;
            x += _movingRight ? 1 : 0;

            var y = 0;
            y -= _movingDown ? 1 : 0;
            y += _movingUp ? 1 : 0;

            var result = new Vector2(x, y);

            // can't normalize zero length vector
            if (result.LengthSquared > 1.0e-6)
            {
                result = result.Normalized;
            }

            return result;
        }

        /// <summary>
        ///     Changes the entity currently controlling this shuttle controller
        /// </summary>
        /// <param name="entity">The entity to set</param>
        private void SetController(IEntity entity)
        {
            if (_controller != null ||
                !entity.TryGetComponent(out MindComponent? mind) ||
                mind.Mind == null ||
                !Owner.TryGetComponent(out ServerAlertsComponent? status))
            {
                return;
            }

            mind.Mind.Visit(Owner);
            _controller = entity;

            status.ShowAlert(_pilotingAlertType);
        }

        /// <summary>
        ///     Removes the current controller
        /// </summary>
        /// <param name="entity">The entity to remove, or null to force the removal of any current controller</param>
        public void RemoveController(IEntity? entity = null)
        {
            if (_controller == null)
            {
                return;
            }

            // If we are not forcing a controller removal and the entity is not the current controller
            if (entity != null && entity != _controller)
            {
                return;
            }

            UpdateRemovedEntity(entity ?? _controller);

            _controller = null;
        }

        /// <summary>
        ///     Updates the state of an entity that is no longer controlling this shuttle controller.
        ///     Called from <see cref="RemoveController"/>
        /// </summary>
        /// <param name="entity">The entity to update</param>
        private void UpdateRemovedEntity(IEntity entity)
        {
            if (Owner.TryGetComponent(out ServerAlertsComponent? status))
            {
                status.ClearAlert(_pilotingAlertType);
            }

            if (entity.TryGetComponent(out MindComponent? mind))
            {
                mind.Mind?.UnVisit();
            }

            if (entity.TryGetComponent(out BuckleComponent? buckle))
            {
                buckle.TryUnbuckle(entity, true);
            }
        }

        private void BuckleChanged(IEntity entity, in bool buckled)
        {
            Logger.DebugS("shuttle", $"Pilot={entity.Name}, buckled={buckled}");

            if (buckled)
            {
                SetController(entity);
            }
            else
            {
                RemoveController(entity);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _pilotingAlertType, "pilotingAlertType", AlertType.PilotingShuttle);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<ServerAlertsComponent>();
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case StrapChangeMessage strap:
                    BuckleChanged(strap.Entity, strap.Buckled);
                    break;
            }
        }
    }
}
