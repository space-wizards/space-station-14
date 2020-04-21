using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetDeathMatch : GamePreset
    {
#pragma warning disable 649
        [Dependency] private readonly IGameTicker _gameTicker;
#pragma warning restore 649

        public override void Start()
        {
            _gameTicker.AddGameRule<RuleDeathMatch>();
        }

        public override string ModeTitle => "Deathmatch";
        public override string Description => "Kill anything that moves!";
    }
}
