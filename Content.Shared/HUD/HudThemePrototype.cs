using Robust.Shared.Prototypes;

namespace Content.Shared.HUD
{
    [Prototype("hudTheme")]
    public readonly record struct HudThemePrototype : IPrototype
    {
        [DataField("name", required: true)]
        public string Name { get; } = string.Empty;

        [IdDataFieldAttribute]
        public string ID { get; } = string.Empty;

        [DataField("path", required: true)]
        public string Path { get; } = string.Empty;
    }
}
