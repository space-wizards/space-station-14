using System.Collections.Generic;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetDeathMatch : GamePreset
    {
#pragma warning disable 649
        [Dependency] private readonly IGameTicker _gameTicker;
#pragma warning restore 649

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            _gameTicker.AddGameRule<RuleDeathMatch>();
            return true;
        }

        public override string ModeTitle => "Deathmatch";
        public override string Description => "Kill anything that moves!";
    }
}
