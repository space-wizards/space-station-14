using System;
using System.Collections.Generic;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Server.Sandbox;
using NFluidsynth;
using Robust.Server.Interfaces.Player;
using Content.Shared.Antags;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Logger = Robust.Shared.Log.Logger;

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
            var numTraitors = Math.Clamp(readyPlayers.Count % PlayersPerTraitor,
                MinTraitors, readyPlayers.Count);

            for (var i = 0; i < numTraitors; i++)
            {
                var traitor = _random.PickAndTake(list);
                var mind = traitor.Data.ContentData().Mind;
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorID);
                mind.AddRole(new Antag(mind, antagPrototype));
            }

            foreach (var player in list)
            {
                var mind = player.Data.ContentData().Mind;
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(InnocentID);
                mind.AddRole(new Antag(mind, antagPrototype));
            }

            _gameTicker.AddGameRule<RuleSuspicion>();
            return true;
        }

        public override string ModeTitle => "Suspicion";
        public override string Description => "Suspicion on the Space Station. There are traitors on board... Can you kill them before they kill you?";
    }
}
