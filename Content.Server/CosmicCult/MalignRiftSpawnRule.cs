using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Light.Components;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.CosmicCult;

public sealed partial class MalignRiftSpawnRule : StationEventSystem<MalignRiftSpawnRuleComponent>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private CosmicRiftSystem _malignRift = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

    protected override void Started(EntityUid uid, MalignRiftSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (_ticker.IsGameRuleActive<CosmicCultRuleComponent>())
        {
            _ticker.EndGameRule(uid); // Cosmic cult's active! Don't actually proceed to the contents of the gamerule!
        }
        else
        {
            var totalCrew = _playerMan.Sessions.Count(session => session.Status == SessionStatus.InGame && HasComp<HumanoidProfileComponent>(session.AttachedEntity));
            var sender = Loc.GetString("cosmiccult-announcement-sender");

            _chatSystem.DispatchStationAnnouncement(chosenStation.Value, Loc.GetString("cosmiccult-announce-tier2-progress"), sender, false, null, Color.FromHex("#4cabb3"));
            _chatSystem.DispatchStationAnnouncement(chosenStation.Value, Loc.GetString("cosmiccult-announce-tier2-warning"), null, false, null, Color.FromHex("#cae8e8"));
            _audio.PlayGlobal(comp.Tier2Sound, Filter.Broadcast(), false, AudioParams.Default);

            for (var i = 0; i < Convert.ToInt16(totalCrew / 6); i++) // spawn # malign rifts equal to 16.67% of the playercount
            {
                _malignRift.SpawnRift(chosenStation.Value);
            }
        }
    }
}
