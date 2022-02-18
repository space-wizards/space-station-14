using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Decals
{
    [Prototype("decal")]
    public sealed class DecalPrototype : IPrototype
    {
        [DataField("id")] public string ID { get; } = null!;
        [DataField("sprite")] public SpriteSpecifier Sprite { get; } = SpriteSpecifier.Invalid;
        [DataField("tags")] public List<string> Tags = new();
    }
}
