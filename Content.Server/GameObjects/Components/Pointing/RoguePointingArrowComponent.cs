#nullable enable
using System.Linq;
using Content.Server.Explosions;
using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Server.GameObjects.Components.Pointing
{
    [RegisterComponent]
    public class RoguePointingArrowComponent : SharedRoguePointingArrowComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [ViewVariables]
        private IEntity? _chasing;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("turningDelay")]
        private float _turningDelay = 2;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingDelay")]
        private float _chasingDelay = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingSpeed")]
        private float _chasingSpeed = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingTime")]
        private float _chasingTime = 1;

        private IEntity? RandomNearbyPlayer()
        {
            var players = _playerManager
                .GetPlayersInRange(Owner.Transform.Coordinates, 15)
                .Where(player => player.AttachedEntity != null)
                .ToArray();

            if (players.Length == 0)
            {
                return null;
            }

            return _random.Pick(players).AttachedEntity;
        }

        private void UpdateAppearance()
        {
            if (_chasing == null ||
                !Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            appearance.SetData(RoguePointingArrowVisuals.Rotation, Owner.Transform.LocalRotation.Degrees);
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
            _chasing ??= RandomNearbyPlayer();

            if (_chasing == null)
            {
                Owner.Delete();
                return;
            }

            _turningDelay -= frameTime;

            if (_turningDelay > 0)
            {
                var difference = _chasing.Transform.WorldPosition - Owner.Transform.WorldPosition;
                var angle = difference.ToAngle();
                var adjusted = angle.Degrees + 90;
                var newAngle = Angle.FromDegrees(adjusted);

                Owner.Transform.LocalRotation = newAngle;

                UpdateAppearance();
                return;
            }

            _chasingDelay -= frameTime;

            Owner.Transform.WorldRotation += Angle.FromDegrees(20);

            UpdateAppearance();

            var toChased = _chasing.Transform.WorldPosition - Owner.Transform.WorldPosition;

            Owner.Transform.WorldPosition += toChased * frameTime * _chasingSpeed;

            _chasingTime -= frameTime;

            if (_chasingTime > 0)
            {
                return;
            }

            Owner.SpawnExplosion(0, 2, 1, 1);
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/explosion.ogg", Owner);

            Owner.Delete();
        }
    }
}
