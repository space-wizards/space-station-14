#nullable enable
using System.Linq;
using Content.Server.Explosions;
using Content.Shared.GameObjects.Components.Pointing;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Pointing
{
    public class RoguePointingArrowComponent : Component
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "RoguePointingArrow";

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
                .GetPlayersInRange(Owner.Transform.GridPosition, 15)
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
                !Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                return;
            }

            var direction = (_chasing.Transform.WorldPosition - Owner.Transform.WorldPosition).ToAngle().Degrees + 90;

            appearance.SetData(PointingArrowVisuals.Rotation, direction);
            appearance.SetData(PointingArrowVisuals.Rotation, Owner.Transform.WorldRotation.Degrees);
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
                UpdateAppearance();
                return;
            }

            _chasingDelay -= frameTime;

            Owner.Transform.WorldRotation += Angle.FromDegrees(90 * frameTime);

            UpdateAppearance();

            Owner.Transform.WorldPosition -= (Owner.Transform.WorldPosition - _chasing.Transform.WorldPosition) * frameTime * _chasingSpeed;

            _chasingTime -= frameTime;

            if (_chasingTime <= 0)
            {
                ExplosionHelper.SpawnExplosion(Owner.Transform.GridPosition, 5, 3, 2, 1);
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/explosion.ogg", Owner);

                Owner.Delete();
            }
        }
    }
}
