using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Decals
{
    [Prototype("decal")]
    public readonly record struct DecalPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = null!;
        [DataField("sprite")] public SpriteSpecifier Sprite { get; } = SpriteSpecifier.Invalid;
        [DataField("tags")] public readonly List<string> Tags = new();
        [DataField("showMenu")] public readonly bool ShowMenu = true;

        /// <summary>
        /// If the decal is rotated compared to our eye should we snap it to south.
        /// </summary>
        [DataField("snapCardinals")] public readonly bool SnapCardinals;
    }
}
