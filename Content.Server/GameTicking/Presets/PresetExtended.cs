using System.Collections.Generic;
using Robust.Server.Player;

namespace Content.Server.GameTicking.Presets
{
    [GamePresetPrototype("extended")]
    public class PresetExtended : GamePresetPrototype
    {
        public override string Description => "No antagonists, have fun!";
        public override string ModeTitle => "Extended";

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            // We do nothing. This is extended after all...
            return true;
        }
    }
}
