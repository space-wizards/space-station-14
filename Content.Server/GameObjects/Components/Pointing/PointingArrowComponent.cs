#nullable enable
using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Server.GameObjects.Components.Pointing
{
    [RegisterComponent]
    public class PointingArrowComponent : SharedPointingArrowComponent
    {
        /// <summary>
        ///     The current amount of seconds left on this arrow.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _duration;

        /// <summary>
        ///     The amount of seconds before the arrow changes movement direction.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _step;

        /// <summary>
        ///     The amount of units that this arrow will move by when multiplied
        ///     by the frame time.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _speed;

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
        private bool _rogue;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _duration, "duration", 4);
            serializer.DataField(ref _step, "step", 0.5f);
            serializer.DataField(ref _speed, "speed", 1);
            serializer.DataField(ref _rogue, "rogue", false);
        }

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
