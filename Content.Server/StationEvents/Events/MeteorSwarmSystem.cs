using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MeteorSwarmSystem : GameRuleSystem<MeteorSwarmComponent>
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly StationSystem _station = default!;

    protected override void Added(EntityUid uid, MeteorSwarmComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.WaveCounter = component.Waves.Next(RobustRandom);

        if (component.Announcement is { } locId)
            _chat.DispatchGlobalAnnouncement(Loc.GetString(locId), playSound: false, colorOverride: Color.Gold);
        _audio.PlayGlobal(component.AnnouncementSound, Filter.Broadcast(), true);
    }

    protected override void ActiveTick(EntityUid uid, MeteorSwarmComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime < component.NextWaveTime)
            return;

        component.NextWaveTime += TimeSpan.FromSeconds(component.WaveCooldown.Next(RobustRandom));


        if (_station.GetStations().Count == 0)
            return;

        var station = RobustRandom.Pick(_station.GetStations());
        if (_station.GetLargestGrid(Comp<StationDataComponent>(station)) is not { } grid)
            return;

        var mapId = Transform(grid).MapID;
        var playableArea = _physics.GetWorldAABB(grid);

        var minimumDistance = (playableArea.TopRight - playableArea.Center).Length() + 50f;
        var maximumDistance = minimumDistance + 100f;

        var center = playableArea.Center;

        var meteorsToSpawn = component.MeteorsPerWave.Next(RobustRandom);
        for (var i = 0; i < meteorsToSpawn; i++)
        {
            var spawnProto = RobustRandom.Pick(component.Meteors);

            var angle = component.NonDirectional
                ? RobustRandom.NextAngle()
                : new Random(uid.Id).NextAngle();

            var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * RobustRandom.NextFloat() + minimumDistance, 0));
            var subOffset = RobustRandom.NextAngle().RotateVec(new Vector2( (playableArea.TopRight - playableArea.Center).Length() / 2 * RobustRandom.NextFloat(), 0));
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
