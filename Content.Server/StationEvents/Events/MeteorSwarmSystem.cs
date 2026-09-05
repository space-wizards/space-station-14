using System.Numerics;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class MeteorSwarmSystem : GameRuleSystem<MeteorSwarmComponent>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ServerStationSystem _station = default!;

    protected override void Added(Entity<MeteorSwarmComponent, GameRuleComponent> rule, ref GameRuleAddedEvent args)
    {
        base.Added(rule, ref args);
        var stations = _station.GetStations();
        if (stations.Count == 0)
            return;

        var station = RobustRandom.Pick(stations);
        if (_station.GetLargestGrid(station.AsNullable()) is not { } grid)
            return;

        rule.Comp1.TargetGrid = grid;
        rule.Comp1.WaveCounter = rule.Comp1.Waves.Next(RobustRandom);

        if (rule.Comp1.Announcement is { } locId)
            GameTicker.StationMapAnnouncement(station, locId, rule.Comp1.AnnouncementSound, Color.Gold);
    }

    protected override void ActiveTick(EntityUid uid, MeteorSwarmComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime < component.NextWaveTime)
            return;

        if (component.TargetGrid is not { } grid)
        {
            ForceEndSelf((uid, gameRule));
            return;
        }

        component.NextWaveTime += TimeSpan.FromSeconds(component.WaveCooldown.Next(RobustRandom));

        var mapId = Transform(grid).MapID;
        var playableArea = _physics.GetWorldAABB(grid);

        var minimumDistance = (playableArea.TopRight - playableArea.Center).Length() + 50f;
        var maximumDistance = minimumDistance + 100f;

        var center = playableArea.Center;

        IRobustRandom random;
        if (component.NonDirectional)
        {
            random = RobustRandom;
        }
        else
        {
            random = new RobustRandom();
            random.SetSeed(uid.Id);
        }

        var meteorsToSpawn = component.MeteorsPerWave.Next(RobustRandom);
        for (var i = 0; i < meteorsToSpawn; i++)
        {
            var spawnProto = RobustRandom.Pick(component.Meteors);

            var angle = random.NextAngle();

            var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * RobustRandom.NextFloat() + minimumDistance, 0));

            // the line at which spawns occur is perpendicular to the offset.
            // This means the meteors are less likely to bunch up and hit the same thing.
            var subOffsetAngle = RobustRandom.Prob(0.5f)
                ? angle + Math.PI / 2
                : angle - Math.PI / 2;
            var subOffset = subOffsetAngle.RotateVec(new Vector2( (playableArea.TopRight - playableArea.Center).Length() / 3 * RobustRandom.NextFloat(), 0));

            var spawnPosition = new MapCoordinates(center + offset + subOffset, mapId);
            var meteor = Spawn(spawnProto, spawnPosition);
            var physics = Comp<PhysicsComponent>(meteor);
            _physics.ApplyLinearImpulse(meteor, -offset.Normalized() * component.MeteorVelocity * physics.Mass, body: physics);
        }

        component.WaveCounter--;
        if (component.WaveCounter <= 0)
        {
            ForceEndSelf(uid, gameRule);
        }
    }
}
