using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    class ShuttleControllerComponent : Component, IMoverComponent
    {
        [Dependency] IMapManager mapManager;
        [Dependency] IEntityManager entityManager;
        [Dependency] IGameTiming gameTiming;

        private bool _movingUp;
        private bool _movingDown;
        private bool _movingLeft;
        private bool _movingRight;

        public override string Name => "ShuttleController";

        [ViewVariables(VVAccess.ReadWrite)]
        public float WalkMoveSpeed { get; set; } = 8;
        public float SprintMoveSpeed { get; set; }
        public bool Sprinting { get; set; }
        public Vector2 VelocityDir { get; }
        public GridCoordinates LastPosition { get; set; }
        public float StepSoundDistance { get; set; }

        public void SetVelocityDirection(Direction direction, bool enabled)
        {
            var gridId = Owner.Transform.GridID;

            if (mapManager.TryGetGrid(gridId, out var grid) && entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                //TODO: Switch to shuttle component
                if (!gridEntity.TryGetComponent(out PhysicsComponent physComp))
                {
                    physComp = gridEntity.AddComponent<PhysicsComponent>();
                    physComp.Mass = 1;
                }

                physComp.LinearVelocity = CalcNewVelocity(direction, enabled) * WalkMoveSpeed;
            }
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
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            if(netChannel != null)
                return;

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
                mindComp.Mind.Visit(Owner);
            }
            else
            {
                mindComp.Mind.UnVisit();
            }
        }
    }
}
