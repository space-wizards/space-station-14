using System;
using System.Collections.Generic;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype("gamePreset")]
    public sealed class GamePresetPrototype : IPrototype
    {
        [DataField("id", required:true)]
        public string ID { get; } = default!;

        [DataField("alias")]
        public string[] Alias { get; } = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle { get; } = "????";

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("showInVote")]
        public bool ShowInVote { get; } = false;

        [DataField("rules", customTypeSerializer:typeof(PrototypeIdListSerializer<GameRulePrototype>))]
        public IReadOnlyList<string> Rules { get; } = Array.Empty<string>();
    }
}
