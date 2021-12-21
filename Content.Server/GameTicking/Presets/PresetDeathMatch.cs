using System.Collections.Generic;
using Content.Server.GameTicking.Rules;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.Presets
{
    [GamePreset("deathmatch")]
    public sealed class PresetDeathMatch : GamePreset
    {
        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            EntitySystem.Get<GameTicker>().AddGameRule<RuleDeathMatch>();
            return true;
        }

        public override string ModeTitle => "Deathmatch";
        public override string Description => "Kill anything that moves!";
    }
}
