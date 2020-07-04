using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared.Antags;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;
using System.Threading.Tasks;
using Content.Shared.Preferences;



namespace Content.Server.GameTicking.GamePresets
{
    public class PresetSuspicion : GamePreset
    {
#pragma warning disable 649
        [Dependency] private readonly IChatManager _chatManager;
        [Dependency] private readonly IGameTicker _gameTicker;
        [Dependency] private readonly IRobustRandom _random;
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public int MinPlayers { get; set; } = 5;
        public int MinTraitors { get; set; } = 2;
        public int PlayersPerTraitor { get; set; } = 5;
        private static string TraitorID = "SuspicionTraitor";
        private static string InnocentID = "SuspicionInnocent";

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            if (!force && readyPlayers.Count < MinPlayers)
            {
                _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {readyPlayers.Count} players readied up out of {MinPlayers} needed.");
                return false;
            }

            var list = new List<IPlayerSession>(readyPlayers);
            var prefList = new List<IPlayerSession>();

            foreach (var player in list)
            {
                if (!readyProfiles.ContainsKey(player.Name))
                {
                    continue;
                }
                var profile = readyProfiles[player.Name];
                if (profile.AntagPreferences.Contains(_prototypeManager.Index<AntagPrototype>(TraitorID).Name))
                {
                    prefList.Add(player);
                }
            }

            var numTraitors = Math.Clamp(readyPlayers.Count % PlayersPerTraitor,
                MinTraitors, readyPlayers.Count);

            for (var i = 0; i < numTraitors; i++)
            {
                IPlayerSession traitor;
                if(prefList.Count() == 0)
                {
                    traitor = _random.PickAndTake(list);
                    Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
                }
                else
                {
                    traitor = _random.PickAndTake(prefList);
                    list.Remove(traitor);
                    Logger.InfoS("preset", "Selected a preferred traitor.");
                }
                var mind = traitor.Data.ContentData().Mind;
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorID);
                mind.AddRole(new SuspicionTraitorRole(mind, antagPrototype));
            }

            foreach (var player in list)
            {
                var mind = player.Data.ContentData().Mind;
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(InnocentID);
                mind.AddRole(new SuspicionInnocentRole(mind, antagPrototype));
            }

            _gameTicker.AddGameRule<RuleSuspicion>();
            return true;
        }

        public override string ModeTitle => "Suspicion";
        public override string Description => "Suspicion on the Space Station. There are traitors on board... Can you kill them before they kill you?";
    }
}
