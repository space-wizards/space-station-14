using Robust.Shared.Prototypes;

namespace Content.Shared.HUD
{
    [Prototype("hudTheme")]
    public sealed partial class HudThemePrototype : IPrototype
    {
        [DataField("name", required: true)]
        public string Name { get; private set; } = string.Empty;

        [IdDataField]
        public string ID { get; private set; } = string.Empty;

        [DataField("path", required: true)]
        public string Path { get; private set; } = string.Empty;
    }
}
