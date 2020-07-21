#nullable enable
using System.Linq;
using Content.Server.Explosions;
using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Server.GameObjects.Components.Pointing
{
    [RegisterComponent]
    public class PointingArrowComponent : SharedPointingArrowComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
#pragma warning restore 649

        /// <summary>
        ///     The current amount of seconds left on this arrow.
        /// </summary>
        private float _duration;

        /// <summary>
        ///     The amount of seconds before the arrow changes movement direction.
        /// </summary>
        private float _step;

        /// <summary>
        ///     The amount of units that this arrow will move by when multiplied
        ///     by the frame time.
        /// </summary>
        private float _speed;

        private bool _rogue;

        /// <summary>
        ///     The current amount of seconds left before the arrow changes
        ///     movement direction.
        /// </summary>
        private float _currentStep;

        /// <summary>
        ///     Whether or not this arrow is currently going up.
        /// </summary>
        private bool _up;

        private IEntity? _chasing;

        private float _turningDelay;

        private float _chasingDelay;

        private float _chasingSpeed;

        private float _chasingTime;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _duration, "duration", 4);
            serializer.DataField(ref _step, "step", 0.5f);
            serializer.DataField(ref _speed, "speed", 1);
            serializer.DataField(ref _rogue, "rogue", false);
            serializer.DataField(ref _turningDelay, "turningDelay", 2);
            serializer.DataField(ref _chasingDelay, "chasingDelay", 1);
            serializer.DataField(ref _chasingSpeed, "chasingSpeed", 5);
            serializer.DataField(ref _chasingTime, "chasingTime", 1f);
        }

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }

        private bool TryChase(float frameTime)
        {
            if (_chasing == null ||
                !Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                return false;
            }

            _turningDelay -= frameTime;

            if (_turningDelay > 0)
            {
                var direction = (_chasing.Transform.WorldPosition - Owner.Transform.WorldPosition).ToAngle().Degrees + 90;

                appearance.SetData(PointingArrowVisuals.Rotation, direction);

                return true;
            }

            _chasingDelay -= frameTime;

            Owner.Transform.WorldRotation += Angle.FromDegrees(90 * frameTime);

            appearance.SetData(PointingArrowVisuals.Rotation, Owner.Transform.WorldRotation.Degrees);

            Owner.Transform.WorldPosition -= (Owner.Transform.WorldPosition - _chasing.Transform.WorldPosition) * frameTime * _chasingSpeed;

            _chasingTime -= frameTime;

            if (_chasingTime <= 0)
            {
                ExplosionHelper.SpawnExplosion(Owner.Transform.GridPosition, 5, 3, 2, 1);
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/explosion.ogg", Owner);

                Owner.Delete();
            }

            return true;
        }

        public void Update(float frameTime)
        {
            if (TryChase(frameTime))
            {
                return;
            }

            var movement = _speed * frameTime * (_up ? 1 : -1);
            Owner.Transform.LocalPosition += (0, movement);

            _duration -= frameTime;
            _currentStep -= frameTime;

            if (_duration <= 0)
            {
                if (_rogue)
                {
                    var players = _playerManager
                        .GetPlayersInRange(Owner.Transform.GridPosition, 15)
                        .Where(player => player.AttachedEntity != null)
                        .ToArray();

                    if (players.Length == 0)
                    {
                        Owner.Delete();
                        return;
                    }

                    _chasing = _random.Pick(players).AttachedEntity;
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
