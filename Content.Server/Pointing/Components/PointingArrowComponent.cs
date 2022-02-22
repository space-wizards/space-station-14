using Content.Shared.Pointing.Components;
using Robust.Server.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPointingArrowComponent))]
    public sealed class PointingArrowComponent : SharedPointingArrowComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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

            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }

        public void Update(float frameTime)
        {
            var movement = _speed * frameTime * (_up ? 1 : -1);
            _entMan.GetComponent<TransformComponent>(Owner).LocalPosition += (0, movement);

            _duration -= frameTime;
            _currentStep -= frameTime;

            if (_duration <= 0)
            {
                if (_rogue)
                {
                    _entMan.RemoveComponent<PointingArrowComponent>(Owner);
                    _entMan.AddComponent<RoguePointingArrowComponent>(Owner);
                    return;
                }

                _entMan.DeleteEntity(Owner);
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
