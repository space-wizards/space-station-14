using Robust.Shared.Prototypes;

namespace Content.Shared.AirlockPainter.Prototypes
{
    [Prototype("AirlockGroup")]
    public readonly record struct AirlockGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("stylePaths")] public readonly Dictionary<string, string> StylePaths = default!;

        // The priority determines, which sprite is used when showing
        // the icon for a style in the airlock painter UI. The highest priority
        // gets shown.
        [DataField("iconPriority")] public readonly int IconPriority;
    }
}
