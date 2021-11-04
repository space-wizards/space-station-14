using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Decals
{
    public class DecalPrototype : IPrototype
    {
        [DataField("id")] public string ID { get; } = null!;
        [DataField("sprite")] public SpriteSpecifier Sprite { get; } = SpriteSpecifier.Invalid;
    }
}
