using Content.Server.GameTicking;
using Content.Shared.Spawners.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.StationEvents.Events
{
    public sealed class MeteorSwarm : StationEventSystem
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        public override string Prototype => "MeteorSwarm";

        private float _cooldown;

        /// <summary>
        /// We'll send a specific amount of waves of meteors towards the station per ending rather than using a timer.
        /// </summary>
        private int _waveCounter;

        private const int MinimumWaves = 3;
        private const int MaximumWaves = 8;

        private const float MinimumCooldown = 10f;
        private const float MaximumCooldown = 60f;

        private const int MeteorsPerWave = 5;
        private const float MeteorVelocity = 10f;
        private const float MaxAngularVelocity = 0.25f;
        private const float MinAngularVelocity = -0.25f;

        public override void Started()
        {
            base.Started();
            var mod = Math.Sqrt(GetSeverityModifier());
            _waveCounter = (int) (RobustRandom.Next(MinimumWaves, MaximumWaves) * mod);
        }

        public override void Ended()
        {
            base.Ended();
            _waveCounter = 0;
            _cooldown = 0f;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (_waveCounter <= 0)
            {
                ForceEndSelf();
                return;
            }

            var mod = GetSeverityModifier();

            _cooldown -= frameTime;

            if (_cooldown > 0f)
                return;

            _waveCounter--;

            _cooldown += (MaximumCooldown - MinimumCooldown) * RobustRandom.NextFloat() / mod + MinimumCooldown;

            Box2? playableArea = null;
            var mapId = GameTicker.DefaultMap;

            foreach (var grid in MapManager.GetAllMapGrids(mapId))
            {
                var aabb = _physics.GetWorldAABB(grid.Owner);
                playableArea = playableArea?.Union(aabb) ?? aabb;
            }

            if (playableArea == null)
            {
                ForceEndSelf();
                return;
            }

            var minimumDistance = (playableArea.Value.TopRight - playableArea.Value.Center).Length + 50f;
            var maximumDistance = minimumDistance + 100f;

            var center = playableArea.Value.Center;

            for (var i = 0; i < MeteorsPerWave; i++)
            {
                var angle = new Angle(RobustRandom.NextFloat() * MathF.Tau);
                var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * RobustRandom.NextFloat() + minimumDistance, 0));
                var spawnPosition = new MapCoordinates(center + offset, mapId);
                var meteor = EntityManager.SpawnEntity("MeteorLarge", spawnPosition);
                var physics = EntityManager.GetComponent<PhysicsComponent>(meteor);
                _physics.SetBodyStatus(physics, BodyStatus.InAir);
                _physics.SetLinearDamping(physics, 0f);
                _physics.SetAngularDamping(physics, 0f);
                _physics.ApplyLinearImpulse(meteor, -offset.Normalized * MeteorVelocity * physics.Mass, body: physics);
                _physics.ApplyAngularImpulse(
                    meteor,
                    physics.Mass * ((MaxAngularVelocity - MinAngularVelocity) * RobustRandom.NextFloat() + MinAngularVelocity),
                    body: physics);

                EnsureComp<TimedDespawnComponent>(meteor).Lifetime = 120f;
            }
        }
    }
}
