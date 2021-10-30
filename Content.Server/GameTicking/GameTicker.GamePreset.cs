using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Content.Server.GameTicking.Presets;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        public const float PresetFailedCooldownIncrease = 30f;

        [ViewVariables] private Type? _presetType;

        [ViewVariables]
        public GamePreset? Preset
        {
            get => _preset ?? MakeGamePreset(new Dictionary<NetUserId, HumanoidCharacterProfile>());
            set => _preset = value;
        }

        public ImmutableDictionary<string, Type> Presets { get; private set; } = default!;

        private GamePreset? _preset;

        private void InitializeGamePreset()
        {
            var presets = new Dictionary<string, Type>();

            foreach (var type in _reflectionManager.FindTypesWithAttribute<GamePresetAttribute>())
            {
                var attribute = type.GetCustomAttribute<GamePresetAttribute>();

                presets.Add(attribute!.Id.ToLowerInvariant(), type);

                foreach (var alias in attribute.Aliases)
                {
                    presets.Add(alias.ToLowerInvariant(), type);
                }
            }

            Presets = presets.ToImmutableDictionary();

            SetStartPreset(_configurationManager.GetCVar(CCVars.GameLobbyDefaultPreset));
        }

        public bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            return Preset?.OnGhostAttempt(mind, canReturnGlobal) ?? false;
        }

        public bool TryGetPreset(string name, [NotNullWhen(true)] out Type? type)
        {
            name = name.ToLowerInvariant();
            return Presets.TryGetValue(name, out type);
        }

        public void SetStartPreset(Type type, bool force = false)
        {
            // Do nothing if this game ticker is a dummy!
            if (DummyTicker)
                return;

            if (!typeof(GamePreset).IsAssignableFrom(type)) throw new ArgumentException("type must inherit GamePreset");

            _presetType = type;
            UpdateInfoText();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetStartPreset(string name, bool force = false)
        {
            if (!TryGetPreset(name, out var type))
            {
                throw new NotSupportedException($"No preset found with name {name}");
            }

            SetStartPreset(type, force);
        }

        private GamePreset MakeGamePreset(Dictionary<NetUserId, HumanoidCharacterProfile> readyProfiles)
        {
            var preset = _dynamicTypeFactory.CreateInstance<GamePreset>(_presetType ?? typeof(PresetSandbox));
            preset.ReadyProfiles = readyProfiles;
            return preset;
        }
    }
}
