using Robust.Shared.Prototypes;

namespace Content.Shared.AirlockPainter.Prototypes
{
    [Prototype("AirlockGroup")]
    public sealed class AirlockGroupPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("stylePaths")]
        public Dictionary<string, string> StylePaths = default!;
    }
}
