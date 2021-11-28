using Content.Shared.Pointing.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Shared.Map;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    public class PointingArrowComponent : SharedPointingArrowComponent
    {
        /// <summary>
        ///     The coordinates of the entity that is trying to point.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public MapCoordinates _pointerCoord;

        /// <summary>
        ///     The coordinates that the pointer points to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public MapCoordinates _pointedCoord;

        /// <summary>
        ///     Whether or not this arrow is flying to its destination.
        ///     The flying part can be disabled if this variable is set to false before component startup.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool _isFlying = true;

        /// <summary>
        ///     Arrow flying speed multiplier.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flyingSpeed")]
        public float _flyingSpeed = 3;

        /// <summary>
        ///     The current amount of seconds left on this arrow.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("duration")]
        public float _duration = 4;

        /// <summary>
        ///     The amount of seconds before the arrow changes movement direction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("step")]
        public float _step = 0.5f;

        /// <summary>
        ///     The amount of units that this arrow will move by when multiplied
        ///     by the frame time.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float _speed = 1;

        /// <summary>
        ///     The current amount of seconds left before the arrow changes
        ///     movement direction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float _currentStep;

        /// <summary>
        ///     Whether or not this arrow is currently going up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool _up;

        /// <summary>
        ///     Whether or not this arrow will convert into a
        ///     <see cref="RoguePointingArrowComponent"/> when its duration runs out.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rogue")]
        public bool _rogue = default;

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }
    }
}
