
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
        public readonly string[] Alias = Array.Empty<string>();

        [DataField("name")]
        public readonly string ModeTitle = "????";

        [DataField("description")]
        public readonly string Description = string.Empty;

        [DataField("showInVote")]
        public readonly bool ShowInVote;

        [DataField("minPlayers")]
        public readonly int? MinPlayers;

        [DataField("maxPlayers")]
        public readonly int? MaxPlayers;

        [DataField("rules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyList<string> Rules { get; } = Array.Empty<string>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps", customTypeSerializer: typeof(PrototypeIdSerializer<GameMapPoolPrototype>))]
        public readonly string? MapPool;
    }
}
