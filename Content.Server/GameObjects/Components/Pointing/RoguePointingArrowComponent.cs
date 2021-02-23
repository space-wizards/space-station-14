#nullable enable
using System.Linq;
using Content.Server.Explosions;
using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
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
        private float _turningDelay;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _chasingDelay;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _chasingSpeed;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _chasingTime;

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _turningDelay, "turningDelay", 2);
            serializer.DataField(ref _chasingDelay, "chasingDelay", 1);
            serializer.DataField(ref _chasingSpeed, "chasingSpeed", 5);
            serializer.DataField(ref _chasingTime, "chasingTime", 1f);
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
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/explosion.ogg", Owner);

            Owner.Delete();
        }
    }
}
