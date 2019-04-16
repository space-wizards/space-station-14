using Robust.Shared.Log;

namespace Content.Server.GameTicking.GamePresets
{
    public class PresetTraitor : GamePreset
    {
        public override void Start()
        {
            Logger.DebugS("ticker.preset", "Current preset is traitor.");
        }
    }
}
