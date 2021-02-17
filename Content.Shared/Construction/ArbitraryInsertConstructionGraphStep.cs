using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [DataDefinition]
    public abstract class ArbitraryInsertConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("name")] public string Name { get; private set; } = string.Empty;
        [DataField("icon")] public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;
    }
}
