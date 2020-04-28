using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Server.Sandbox;
using NFluidsynth;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameTicking.GamePresets
{
    public class PresetTraitor : GamePreset
    {
#pragma warning disable 649
        [Dependency] private readonly ISandboxManager _sandboxManager;
        [Dependency] private readonly IChatManager _chatManager;
        [Dependency] private readonly IGameTicker _gameTicker;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public int MinPlayers { get; set; } = 2;
        public int MinTraitors { get; set; } = 1;
        public int PlayersPerTraitor { get; set; } = 5;

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers)
        {
            if (readyPlayers.Count < MinPlayers)
            {
                _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {readyPlayers.Count} players readied up out of {MinPlayers} needed.");
                return false;
            }

            var list = new List<IPlayerSession>(readyPlayers);
            var numTraitors = Math.Max(readyPlayers.Count() % PlayersPerTraitor, MinTraitors);

            for (var i = 0; i < numTraitors; i++)
            {
                var traitor = _random.PickAndTake(list);
                var mind = traitor.Data.ContentData().Mind;
                mind.AddRole(new TraitorRole(mind));
            }

            foreach (var player in list)
            {
                var mind = player.Data.ContentData().Mind;
                mind.AddRole(new InnocentRole(mind));
            }

            _sandboxManager.IsSandboxEnabled = false;
            _gameTicker.AddGameRule<RuleTraitor>();
            return true;
        }

        public override string ModeTitle => "Traitor";
        public override string Description => "There are traitors on board... Can you kill them before they kill you?";
    }
}
