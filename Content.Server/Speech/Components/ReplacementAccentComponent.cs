using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Speech.Components
{
    [Prototype("accent")]
    public sealed partial class ReplacementAccentPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     If this array is non-null, the full text of anything said will be randomly replaced with one of these words.
        /// </summary>
        [DataField("fullReplacements")]
        public string[]? FullReplacements;

        /// <summary>
        ///     If this dictionary is non-null and <see cref="FullReplacements"/> is null, any keys surrounded by spaces
        ///     (words) will be replaced by the value, attempting to intelligently keep capitalization.
        /// </summary>
        [DataField("wordReplacements")]
        public Dictionary<string, string>? WordReplacements;

        /// <summary>
        /// Allows you to substitute words, not always, but with some chance
        /// </summary>
        [DataField]
        public float ReplacementChance = 1f;
    }

    /// <summary>
    /// Replaces full sentences or words within sentences with new strings.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ReplacementAccentComponent : Component
    {
        [DataField("accent", customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>), required: true)]
        public string Accent = default!;

    }
}
