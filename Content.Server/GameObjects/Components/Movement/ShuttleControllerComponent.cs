using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    internal class ShuttleControllerComponent : Component, IMoverComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private bool _movingUp;
        private bool _movingDown;
        private bool _movingLeft;
        private bool _movingRight;

        public override string Name => "ShuttleController";

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentWalkSpeed { get; set; } = 8;
        public float CurrentSprintSpeed { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public float CurrentPushSpeed => 0.0f;

        /// <inheritdoc />
        [ViewVariables]
        public float GrabRange => 0.0f;

        public bool Sprinting { get; set; }
        public (Vector2 walking, Vector2 sprinting) VelocityDir { get; } = (Vector2.Zero, Vector2.Zero);
        public GridCoordinates LastPosition { get; set; }
        public float StepSoundDistance { get; set; }

        public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
        {
            var gridId = Owner.Transform.GridID;

            if (_mapManager.TryGetGrid(gridId, out var grid) && _entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                //TODO: Switch to shuttle component
                if (!gridEntity.TryGetComponent(out PhysicsComponent physComp))
                {
                    physComp = gridEntity.AddComponent<PhysicsComponent>();
                    physComp.Mass = 1;
                }

                //TODO: Is this always true?
                if (!gridEntity.HasComponent<ICollidableComponent>())
                {
                    var collideComp = gridEntity.AddComponent<CollidableComponent>();
                    collideComp.CanCollide = true;
                    //collideComp.IsHardCollidable = true;
                    collideComp.PhysicsShapes.Add(new PhysShapeGrid(grid));
                }

                physComp.LinearVelocity = CalcNewVelocity(direction, enabled) * CurrentWalkSpeed;
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
                result = result.Normalized;

            return result;
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case ContainerContentsModifiedMessage contents:
                    if(contents.Entity.TryGetComponent(out MindComponent mindComp))
                        ContentsChanged(contents.Entity, mindComp, contents.Removed);
                    break;
            }
        }

        private void ContentsChanged(IEntity entity, MindComponent mindComp, in bool removed)
        {
            Logger.DebugS("shuttle", $"Pilot={entity.Name}, removed={removed}");

            if (!removed)
            {
                mindComp.Mind?.Visit(Owner);
            }
            else
            {
                mindComp.Mind?.UnVisit();
            }
        }
    }
}
