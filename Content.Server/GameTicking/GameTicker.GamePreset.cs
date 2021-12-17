using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules;
using Content.Shared.CCVar;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        public const float PresetFailedCooldownIncrease = 30f;

        private GamePresetPrototype? _preset;

        private void InitializeGamePreset()
        {
            SetGamePreset(_configurationManager.GetCVar(CCVars.GameLobbyDefaultPreset));
        }

        private bool AddGamePresetRules()
        {
            if (_preset == null)
                return false;

            foreach (var rule in _preset.Rules)
            {
                if (!_prototypeManager.TryIndex(rule, out GameRulePrototype? ruleProto))
                    continue;

                AddGameRule(ruleProto);
            }

            return true;
        }

        public bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            return Preset?.OnGhostAttempt(mind, canReturnGlobal) ?? false;
        }

        public void SetGamePreset(GamePresetPrototype preset, bool force = false)
        {
            // Do nothing if this game ticker is a dummy!
            if (DummyTicker)
                return;

            _preset = preset;
            UpdateInfoText();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetGamePreset(string preset, bool force = false)
        {
            SetGamePreset(_prototypeManager.Index<GamePresetPrototype>(preset), force);
        }
    }
}
