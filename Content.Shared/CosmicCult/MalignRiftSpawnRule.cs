using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.CosmicCult.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Humanoid;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.CosmicCult;

public sealed partial class MalignRiftSpawnRule : StationEventSystem<MalignRiftSpawnRuleComponent>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private CosmicRiftSystem _malignRift = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

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

            for (var i = 0; i < Convert.ToInt16(totalCrew / 6); i++) // spawn # malign rifts equal to 16.67% of the playercount
            {
                _malignRift.SpawnRift(chosenStation.Value.Owner);
            }
        }
    }
}
