using System.Linq;
using Content.Shared.Chat;
using Content.Shared.CosmicCult.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Humanoid;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult;

public sealed partial class MalignRiftSpawnRule : GameRuleSystem<MalignRiftSpawnRuleComponent>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chatSystem = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ISharedPlayerManager _playerMan = default!;

    public static readonly EntProtoId MalignRiftEntity = "CosmicMalignRift";

    protected override void Started(Entity<MalignRiftSpawnRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        if (!_station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        if (_ticker.IsGameRuleActive<CosmicCultRuleComponent>())
        {
            _ticker.EndGameRule((rule, rule.Comp2)); // Cosmic cult's active! Don't actually proceed to the contents of the gamerule!
        }
        else
        {
            var totalCrew = _playerMan.Sessions.Count(session => session.Status == SessionStatus.InGame && HasComp<HumanoidProfileComponent>(session.AttachedEntity));
            var sender = Loc.GetString("cosmiccult-announcement-sender");

            _chatSystem.DispatchStationAnnouncement(chosenStation.Value, Loc.GetString("cosmiccult-announce-tier2-progress"), sender, false, null, Color.FromHex("#4cabb3"));
            _chatSystem.DispatchStationAnnouncement(chosenStation.Value, Loc.GetString("cosmiccult-announce-tier2-warning"), null, false, null, Color.FromHex("#cae8e8"));
            _audio.PlayGlobal(rule.Comp1.Tier2Sound, Filter.Broadcast(), false, AudioParams.Default);

            #if DEBUG
            if (totalCrew < 25)
            {
                totalCrew = 25;
                Log.Debug("Debug mode. Malign Rifts spawning as if Player Count is 25.");
            }
            #endif

            for (var i = 0; i < (short) totalCrew / 6; i++) // spawn # malign rifts equal to 16.67% of the playercount
            {
                if (_station.TryFindRandomTileOnStation((chosenStation.Value.Owner, chosenStation.Value.Comp1), out var _, out var _, out var coords))
                    Spawn(MalignRiftEntity, coords);
            }

            var devices = EntityQueryEnumerator<CosmicLambdaDeviceComponent>();
            while (devices.MoveNext(out var uid, out _))
            {
                var evt = new CosmicDeviceUpgradeEvent();
                RaiseLocalEvent(uid, ref evt);
            }
        }
    }
}
