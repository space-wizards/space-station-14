using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Prototypes.HUD
{
    [Prototype("hudTheme")]
    public class HudThemePrototype : IPrototype
    {
        [DataField("name", required: true)]
        public string Name { get; } = string.Empty;

        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        [DataField("path", required: true)]
        public string Path { get; } = string.Empty;
    }
}
