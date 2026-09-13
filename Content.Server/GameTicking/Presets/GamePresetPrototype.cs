using Content.Server.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype]
    public sealed partial class GamePresetPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField]
        public string[] Alias = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle = "????";

        [DataField]
        public string Description = string.Empty;

        [DataField]
        public bool ShowInVote;

        [DataField]
        public int? MinPlayers;

        [DataField]
        public int? MaxPlayers;

        [DataField]
        public IReadOnlyList<EntProtoId> Rules { get; private set; } = Array.Empty<EntProtoId>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps")]
        public ProtoId<GameMapPoolPrototype>? MapPool;
    }
}
