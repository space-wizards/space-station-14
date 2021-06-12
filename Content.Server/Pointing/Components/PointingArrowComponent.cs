#nullable enable
using Content.Shared.Pointing.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    public class PointingArrowComponent : SharedPointingArrowComponent
    {
        /// <summary>
        ///     The current amount of seconds left on this arrow.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("duration")]
        private float _duration = 4;

        /// <summary>
        ///     The amount of seconds before the arrow changes movement direction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("step")]
        private float _step = 0.5f;

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

        /// <summary>
        ///     Whether or not this arrow will convert into a
        ///     <see cref="RoguePointingArrowComponent"/> when its duration runs out.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rogue")]
        private bool _rogue = default;

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }

        public void Update(float frameTime)
        {
            var movement = _speed * frameTime * (_up ? 1 : -1);
            Owner.Transform.LocalPosition += (0, movement);

            _duration -= frameTime;
            _currentStep -= frameTime;

            if (_duration <= 0)
            {
                if (_rogue)
                {
                    Owner.RemoveComponent<PointingArrowComponent>();
                    Owner.AddComponent<RoguePointingArrowComponent>();
                    return;
                }

                Owner.Delete();
                return;
            }

            if (_currentStep <= 0)
            {
                _currentStep = _step;
                _up ^= true;
            }
        }
    }
}
