using Content.Shared.Pointing.Components;

namespace Content.Client.Pointing.Components
{
    [RegisterComponent]
    public sealed class PointingArrowComponent : SharedPointingArrowComponent
    {
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
        private float _speed = 1;

        /// <summary>
        ///     The current amount of seconds left before the arrow changes
        ///     movement direction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _currentStep;

        /// <summary>
        ///     Whether or not this arrow is currently going up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _up;
    }
}
