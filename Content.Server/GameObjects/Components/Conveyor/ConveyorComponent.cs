using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Conveyor
{
    // TODO: Start/stop, directional textures
    [RegisterComponent]
    public class ConveyorComponent : Component, ICollideBehavior
    {
        public override string Name => "Conveyor";

        /// <summary>
        ///     The angle in radians to move entities by in relation
        ///     to the owner's rotation.
        ///     Parsed from YAML as degrees.
        /// </summary>
        private double _angle;

        /// <summary>
        ///     The amount of units to move the entity by.
        /// </summary>
        private float _speed;

        private Angle GetAngle()
        {
            return new Angle(Owner.Transform.LocalRotation.Theta + _angle);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            double degrees = 0;
            serializer.DataField(ref degrees, "angle", 0);

            _angle = MathHelper.DegreesToRadians(degrees);

            serializer.DataField(ref _speed, "speed", 0.1f);
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            collidedWith.Transform.LocalPosition += GetAngle().ToVec() * _speed;
        }
    }
}
