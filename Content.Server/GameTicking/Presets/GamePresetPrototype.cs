
using Content.Server.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype("gamePreset")]
    public sealed class GamePresetPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("alias")]
        public string[] Alias { get; private set; } = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle { get; private set; } = "????";

        [DataField("description")]
        public string Description { get; private set; } = string.Empty;

        [DataField("showInVote")]
        public bool ShowInVote { get; private set; }

        [DataField("minPlayers")]
        public int? MinPlayers { get; private set; }

        [DataField("maxPlayers")]
        public int? MaxPlayers { get; private set; }

        [DataField("rules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyList<string> Rules { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps", customTypeSerializer: typeof(PrototypeIdSerializer<GameMapPoolPrototype>))]
        public string? MapPool { get; private set; }
    }
}
