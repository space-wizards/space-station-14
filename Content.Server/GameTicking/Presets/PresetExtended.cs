using System.Collections.Generic;
using Robust.Server.Player;

namespace Content.Server.GameTicking.Presets
{
    [GamePreset("extended")]
    public class PresetExtended : GamePreset
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
