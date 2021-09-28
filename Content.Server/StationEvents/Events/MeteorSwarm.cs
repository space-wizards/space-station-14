using System;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Projectiles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents.Events
{
    public sealed class MeteorSwarm : StationEvent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "MeteorSwarm";

        public override int EarliestStart => 30;
        public override float Weight => WeightLow;
        public override int? MaxOccurrences => 2;
        public override int MinimumPlayers => 20;

        public override string StartAnnouncement => "Meteors are on a collision course with the station. Brace for impact.";
        protected override string EndAnnouncement => "The meteor swarm has passed. Please return to your stations.";

        public override string? StartAudio => "/Audio/Announcements/bloblarm.ogg";

        protected override float StartAfter => 30f;
        protected override float EndAfter => float.MaxValue;

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

        public override void Startup()
        {
            base.Startup();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            _waveCounter = robustRandom.Next(MinimumWaves, MaximumWaves);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _waveCounter = 0;
            _cooldown = 0f;
            EndAfter = float.MaxValue;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Started) return;

            if (_waveCounter <= 0)
            {
                EndAfter = float.MinValue;
                return;
            }
            _cooldown -= frameTime;

            if (_cooldown > 0f) return;

            _waveCounter--;

            _cooldown += (MaximumCooldown - MinimumCooldown) * _robustRandom.NextFloat() + MinimumCooldown;

            Box2? playableArea = null;
            var mapId = EntitySystem.Get<GameTicker>().DefaultMap;

            foreach (var grid in _mapManager.GetAllGrids())
            {
                if (grid.ParentMapId != mapId || !_entityManager.TryGetComponent(grid.GridEntityId, out PhysicsComponent? gridBody)) continue;
                var aabb = gridBody.GetWorldAABB();
                playableArea = playableArea?.Union(aabb) ?? aabb;
            }

            if (playableArea == null)
            {
                EndAfter = float.MinValue;
                return;
            }

            var minimumDistance = (playableArea.Value.TopRight - playableArea.Value.Center).Length + 50f;
            var maximumDistance = minimumDistance + 100f;

            var center = playableArea.Value.Center;

            for (var i = 0; i < MeteorsPerWave; i++)
            {
                var angle = new Angle(_robustRandom.NextFloat() * MathF.Tau);
                var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * _robustRandom.NextFloat() + minimumDistance, 0));
                var spawnPosition = new MapCoordinates(center + offset, mapId);
                var meteor = _entityManager.SpawnEntity("MeteorLarge", spawnPosition);
                var physics = _entityManager.GetComponent<PhysicsComponent>(meteor.Uid);
                physics.BodyStatus = BodyStatus.InAir;
                physics.ApplyLinearImpulse(-offset.Normalized * MeteorVelocity * physics.Mass);
                physics.ApplyAngularImpulse(
                    // Get a random angular velocity.
                    physics.Mass * ((MaxAngularVelocity - MinAngularVelocity) * _robustRandom.NextFloat() +
                                    MinAngularVelocity));
                // TODO: God this disgusts me but projectile needs a refactor.
                meteor.GetComponent<ProjectileComponent>().TimeLeft = 120f;
            }
        }
    }
}
